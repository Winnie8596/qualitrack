# syntax=docker/dockerfile:1

# One image for the whole app: the API, serving the built web app from its web root.
# Three stages, so the final image holds the published output and the .NET runtime, and neither SDK.

# --- Stage 1: build the React app ---
FROM node:22-alpine AS web
WORKDIR /web

# The dependency list first: this layer is rebuilt only when package-lock.json changes, not on every source edit
COPY web/package.json web/package-lock.json ./
RUN npm ci

COPY web/ ./
RUN npm run build

# --- Stage 2: publish the API ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /source

# Project files first, for the same reason: restore is cached until a package reference changes
COPY Directory.Build.props .editorconfig ./
COPY src/QualiTrack.Domain/QualiTrack.Domain.csproj src/QualiTrack.Domain/
COPY src/QualiTrack.Application/QualiTrack.Application.csproj src/QualiTrack.Application/
COPY src/QualiTrack.Infrastructure/QualiTrack.Infrastructure.csproj src/QualiTrack.Infrastructure/
COPY src/QualiTrack.Api/QualiTrack.Api.csproj src/QualiTrack.Api/
RUN dotnet restore src/QualiTrack.Api/QualiTrack.Api.csproj

COPY src/ src/
RUN dotnet publish src/QualiTrack.Api/QualiTrack.Api.csproj --configuration Release --no-restore --output /app

# --- Stage 3: the image that runs ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Npgsql looks for Kerberos (GSSAPI) support when it connects; the slim runtime image has none, and each
# connection would log a loader error. PostgreSQL here uses password authentication, so the library is only
# there to be found.
RUN apt-get update \
    && apt-get install --yes --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

# Photos and the session cookie's keys live under /data, which a volume is mounted over. Created here and owned
# by the unprivileged user, because a new volume takes its ownership from the folder it is mounted on.
RUN mkdir -p /data/photos /data/keys && chown -R $APP_UID /data

COPY --from=api /app ./
COPY --from=web /web/dist ./wwwroot

# TLS ends at the proxy in front (Caddy in docker compose, the ingress in Azure Container Apps), which tells the
# app the original scheme and client address in X-Forwarded-* headers. Trusting them is right only because the
# container is never reachable except through that proxy.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    FileStore__Root=/data/photos \
    DataProtection__KeysPath=/data/keys

# Not root: a bug in the app cannot rewrite the image's own files
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "QualiTrack.Api.dll"]
