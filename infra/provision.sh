#!/usr/bin/env bash
# Creates everything QualiTrack runs on in Azure, in one resource group. Safe to run again: each step
# creates or updates. Read infra/README.md first: it says what this costs and how to remove it.
#
#   POSTGRES_ADMIN_PASSWORD=... DEMO_PASSWORD=... IMAGE=ghcr.io/<owner>/qualitrack:v1.0.0 infra/provision.sh
#
# Needs: Azure CLI, signed in (`az login`) to the subscription to use.
set -euo pipefail

: "${POSTGRES_ADMIN_PASSWORD:?the database administrator password to set}"
: "${DEMO_PASSWORD:?the password of the three demo users (6+ characters: upper, lower, digit, symbol)}"
: "${IMAGE:?the image to run, e.g. ghcr.io/<owner>/qualitrack:v1.0.0 (pushed by .github/workflows/cd.yml)}"

LOCATION="${LOCATION:-japaneast}"
NAME="${NAME:-qualitrack}"
RESOURCE_GROUP="${RESOURCE_GROUP:-$NAME-rg}"

# Database and storage names are global across Azure, so they carry a few characters of the subscription id
SUFFIX="$(az account show --query id --output tsv | tr -d '-' | cut -c1-6)"
DATABASE_SERVER="$NAME-db-$SUFFIX"
STORAGE_ACCOUNT="${NAME}data${SUFFIX}"
ENVIRONMENT="$NAME-env"
SHARE="data"

echo "== Resource group $RESOURCE_GROUP in $LOCATION"
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output none

# A new subscription has not used these services yet
for provider in Microsoft.App Microsoft.OperationalInsights Microsoft.DBforPostgreSQL Microsoft.Storage; do
  az provider register --namespace "$provider" --wait --output none
done

echo "== PostgreSQL $DATABASE_SERVER (smallest burstable size)"
if ! az postgres flexible-server show --name "$DATABASE_SERVER" --resource-group "$RESOURCE_GROUP" --output none 2>/dev/null; then
  # --public-access 0.0.0.0 opens the firewall to Azure's own services only, which is where the app runs.
  # The connection is still TLS with a password; a private network would be the next step up.
  az postgres flexible-server create \
    --name "$DATABASE_SERVER" --resource-group "$RESOURCE_GROUP" --location "$LOCATION" \
    --tier Burstable --sku-name Standard_B1ms --storage-size 32 --version 17 \
    --admin-user qualitrack --admin-password "$POSTGRES_ADMIN_PASSWORD" \
    --database-name qualitrack --public-access 0.0.0.0 --yes --output none
fi
DATABASE_HOST="$(az postgres flexible-server show --name "$DATABASE_SERVER" --resource-group "$RESOURCE_GROUP" \
  --query fullyQualifiedDomainName --output tsv)"

echo "== Storage $STORAGE_ACCOUNT (photos and session keys)"
az storage account create --name "$STORAGE_ACCOUNT" --resource-group "$RESOURCE_GROUP" --location "$LOCATION" \
  --sku Standard_LRS --kind StorageV2 --min-tls-version TLS1_2 --allow-blob-public-access false --output none
STORAGE_KEY="$(az storage account keys list --account-name "$STORAGE_ACCOUNT" --resource-group "$RESOURCE_GROUP" \
  --query "[0].value" --output tsv)"
az storage share create --name "$SHARE" --account-name "$STORAGE_ACCOUNT" --account-key "$STORAGE_KEY" --output none

echo "== Container Apps environment $ENVIRONMENT"
az containerapp env create --name "$ENVIRONMENT" --resource-group "$RESOURCE_GROUP" --location "$LOCATION" \
  --output none
az containerapp env storage set --name "$ENVIRONMENT" --resource-group "$RESOURCE_GROUP" \
  --storage-name "$SHARE" --access-mode ReadWrite \
  --azure-file-account-name "$STORAGE_ACCOUNT" --azure-file-account-key "$STORAGE_KEY" \
  --azure-file-share-name "$SHARE" --output none
ENVIRONMENT_ID="$(az containerapp env show --name "$ENVIRONMENT" --resource-group "$RESOURCE_GROUP" --query id --output tsv)"

echo "== Container app $NAME running $IMAGE"
# The app's definition holds the secrets, so it is written to a temporary file that is removed on exit
DEFINITION="$(mktemp)"
trap 'rm -f "$DEFINITION"' EXIT
cat > "$DEFINITION" <<YAML
location: $LOCATION
properties:
  environmentId: $ENVIRONMENT_ID
  configuration:
    # TLS ends at the ingress; the container listens on plain HTTP and trusts X-Forwarded-* (see Dockerfile)
    ingress:
      external: true
      targetPort: 8080
      allowInsecure: false
    # Secrets live in Container Apps, not in the image or the repository
    secrets:
      - name: database-connection
        value: "Host=$DATABASE_HOST;Port=5432;Database=qualitrack;Username=qualitrack;Password=$POSTGRES_ADMIN_PASSWORD;Ssl Mode=Require"
      - name: demo-password
        value: "$DEMO_PASSWORD"
  template:
    containers:
      - name: app
        image: $IMAGE
        resources:
          cpu: 0.25
          memory: 0.5Gi
        env:
          - name: ConnectionStrings__QualiTrack
            secretRef: database-connection
          - name: DemoUsers__Inspector
            secretRef: demo-password
          - name: DemoUsers__Investigator
            secretRef: demo-password
          - name: DemoUsers__QaManager
            secretRef: demo-password
          # A public demo: it brings its database up to date and fills it on start
          - name: Database__MigrateOnStartup
            value: "true"
          - name: DemoData__Seed
            value: "true"
        probes:
          - type: Liveness
            httpGet:
              path: /health
              port: 8080
            periodSeconds: 30
          - type: Startup
            httpGet:
              path: /health
              port: 8080
            periodSeconds: 5
            failureThreshold: 30
        volumeMounts:
          - volumeName: data
            mountPath: /data
    # Exactly one replica: the dashboard cache and the login rate limit are in its memory (no Redis here),
    # and one is always running so the first visitor does not wait for a cold start
    scale:
      minReplicas: 1
      maxReplicas: 1
    volumes:
      - name: data
        storageType: AzureFile
        storageName: $SHARE
YAML

if az containerapp show --name "$NAME" --resource-group "$RESOURCE_GROUP" --output none 2>/dev/null; then
  az containerapp update --name "$NAME" --resource-group "$RESOURCE_GROUP" --yaml "$DEFINITION" --output none
else
  az containerapp create --name "$NAME" --resource-group "$RESOURCE_GROUP" --yaml "$DEFINITION" --output none
fi

HOST="$(az containerapp show --name "$NAME" --resource-group "$RESOURCE_GROUP" \
  --query properties.configuration.ingress.fqdn --output tsv)"
echo
echo "QualiTrack is at https://$HOST"
echo "Health:        https://$HOST/health"
