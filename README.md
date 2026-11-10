# QualiTrack

[![CI](https://github.com/Winnie8596/qualitrack/actions/workflows/ci.yml/badge.svg)](https://github.com/Winnie8596/qualitrack/actions/workflows/ci.yml)

Track non-conformance reports (NCRs) and their corrective actions from report to verified close,
with every change audited. Built with ASP.NET Core (.NET 10), PostgreSQL and React + TypeScript.

> Work in progress: Phases 0–7 are done; deployment remains. See [`plan.md`](plan.md).

## Run it

Prerequisite: Docker.

```bash
cp .env.example .env    # then change the passwords
docker compose up       # builds the image; first run takes a few minutes
```

- **App:** https://localhost:8443. The browser warns once about the certificate, which comes from
  the local proxy's own authority. Sign in as `inspector@`, `investigator@` or
  `qamanager@qualitrack.demo` with the passwords from `.env`. It starts with 50 demo NCRs.
- **Traces, metrics and logs:** http://localhost:18888 (Aspire Dashboard).

## Develop

Prerequisites: .NET 10 SDK, Node.js 22, Docker.

```bash
docker compose up -d db cache telemetry   # PostgreSQL, Redis and the telemetry viewer only
# export the "dotnet run only" and "Demo users" variables from .env, then:
dotnet run --project src/QualiTrack.Api --launch-profile https   # API on https://localhost:7178
cd web && npm ci && npm run dev                                   # web app on http://localhost:5173
```

## Test

```bash
dotnet test                       # unit and integration tests (integration needs Docker)
cd web
npm run lint && npm test && npm run build
npm run e2e                       # Playwright; needs E2E_CONNECTION_STRING and E2E_DEMO_PASSWORD
```

## Docs

- [`PRD.md`](PRD.md): what is being built and why
- [`plan.md`](plan.md): order of work and engineering rules
- [`docs/adr/`](docs/adr/): architecture decision records
- [`docs/devlog.md`](docs/devlog.md): progress and lessons per phase
