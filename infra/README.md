# Running QualiTrack in Azure

`provision.sh` creates everything in one resource group. The CD workflow then rolls out each new tag.

```text
internet ──HTTPS──> Container Apps ingress (ends TLS)
                         └── container: API + built web app  (image from GitHub Container Registry)
                               ├── Azure Database for PostgreSQL, flexible server
                               └── Azure Files share mounted at /data  (photos, session keys)
```

| Resource | Size | Why |
| --- | --- | --- |
| Container Apps environment + app | 0.25 vCPU, 0.5 GiB, exactly 1 replica | Runs the image; gives it HTTPS and a public address |
| PostgreSQL flexible server | Burstable B1ms, 32 GiB | The database; managed backups and patching |
| Storage account, one file share | Standard LRS | `/data`: NCR photos and the keys that encrypt the session cookie |
| Log Analytics workspace | Created with the environment | Container logs |

There is no Redis: with one replica the dashboard cache lives in the app's memory, which is what the
app does whenever `ConnectionStrings__Redis` is not set.

## Cost

Roughly US$25–30 a month while it runs, most of it the database and the always-on replica. Check
the [pricing calculator](https://azure.microsoft.com/pricing/calculator/) for your region.
**Remove everything, and stop paying, with one command:**

```bash
az group delete --name qualitrack-rg
```

## First deployment

1. Push a tag so the image exists: `git tag v1.0.0 && git push origin v1.0.0`. The `image` job of
   [`cd.yml`](../.github/workflows/cd.yml) pushes `ghcr.io/<owner>/qualitrack:v1.0.0`.
2. Check Azure can pull the image without a password. A package pushed by a workflow of a public
   repository is public already. If it is not: on GitHub, your profile → Packages → `qualitrack` →
   Package settings → Change visibility → Public.
3. Create the resources (about ten minutes):

   ```bash
   az login
   POSTGRES_ADMIN_PASSWORD='...' DEMO_PASSWORD='...' \
   IMAGE=ghcr.io/<owner>/qualitrack:v1.0.0 \
   infra/provision.sh
   ```

   It prints the app's address. `LOCATION` defaults to `japaneast`; a subscription may restrict
   which regions it can use. If Azure creates the environment in "express" mode, which cannot
   mount the file share, the script switches it to standard mode; that alone took twenty minutes.
   If a step fails on a network timeout, run the script again: it carries on where it stopped.
4. Set the repository variable `APP_URL` to that address (Settings → Secrets and variables →
   Actions → Variables) so [`uptime.yml`](../.github/workflows/uptime.yml) starts checking it.

## Let GitHub deploy

So that later tags roll out by themselves, GitHub needs permission to update the container app. It
gets it without any stored password: Azure trusts the identity token GitHub issues to this
repository's `production` environment (OpenID Connect).

```bash
SUBSCRIPTION=$(az account show --query id --output tsv)
APP_ID=$(az ad app create --display-name qualitrack-github --query appId --output tsv)
az ad sp create --id "$APP_ID"
az role assignment create --assignee "$APP_ID" --role Contributor \
  --scope "/subscriptions/$SUBSCRIPTION/resourceGroups/qualitrack-rg"
az ad app federated-credential create --id "$APP_ID" --parameters '{
  "name": "github-production",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:<owner>/qualitrack:environment:production",
  "audiences": ["api://AzureADTokenExchange"]
}'
```

Then set these repository variables: `AZURE_CLIENT_ID` (the `APP_ID`), `AZURE_TENANT_ID`,
`AZURE_SUBSCRIPTION_ID`, `AZURE_RESOURCE_GROUP` (`qualitrack-rg`), `AZURE_CONTAINER_APP`
(`qualitrack`). None of them is a secret. Until they are set, the `deploy` job is skipped and a new
version is rolled out by running `provision.sh` again with the new `IMAGE`.

Some organisation-managed subscriptions (student ones included) do not allow creating app
registrations. In that case keep deploying with `provision.sh`.

## What a production system would add

- Migrations as their own release step (an EF Core migration bundle), not on startup
- The database on a private network instead of "Azure services only"
- Azure Blob Storage for photos and Redis for the cache, so more than one replica can run
- Terraform instead of a script, so changes can be reviewed as a plan before they are applied
