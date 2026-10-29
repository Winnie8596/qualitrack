# QualiTrack

[![CI](https://github.com/Winnie8596/qualitrack/actions/workflows/ci.yml/badge.svg)](https://github.com/Winnie8596/qualitrack/actions/workflows/ci.yml)

Track non-conformance reports (NCRs) and their corrective actions from report to verified close,
with every change audited. Built with ASP.NET Core (.NET 10), PostgreSQL and React + TypeScript.

> Work in progress: Phase 0 (walking skeleton). See [`plan.md`](plan.md) for the roadmap.

## Run locally

Prerequisites: .NET 10 SDK, Node.js 22, Docker.

```bash
cp .env.example .env              # then change POSTGRES_PASSWORD
docker compose up -d --wait       # PostgreSQL 17 on localhost:5432
dotnet run --project src/QualiTrack.Api --launch-profile http    # API on http://localhost:5097
cd web && npm ci && npm run dev   # web app on http://localhost:5173
```

The web page shows **API status: healthy** when it can reach the API.

## Test

```bash
dotnet test
cd web && npm run lint && npm run build
```

## Docs

- [`PRD.md`](PRD.md): what is being built and why
- [`plan.md`](plan.md): order of work and engineering rules
- [`docs/adr/`](docs/adr/): architecture decision records
- [`docs/devlog.md`](docs/devlog.md): progress and lessons per phase
