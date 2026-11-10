# Devlog

What happened in each phase, what was decided, and what was learned. Newest phase first.

## Phase 6 — Cross-cutting

**Built**

- One Docker image for the whole app: a multi-stage `Dockerfile` builds the React app and the API
  separately, and the API serves the built page from its web root. `docker compose up` runs it with
  PostgreSQL, Redis, a Caddy proxy for HTTPS and the Aspire Dashboard for telemetry.
- Startup behaviour by configuration (`Database:MigrateOnStartup`, `DemoData:Seed`) instead of by
  environment name; `.env.example` covers both ways of running.
- A demo seed of 50 NCRs across every status, built through the aggregate's own methods with past
  audit stamps.
- OpenTelemetry: a trace per API request with a span per SQL command, request-duration and
  connection-pool metrics, exported over OTLP where an endpoint is configured.
- [ADR 0003](adr/0003-time-and-user-passed-into-the-domain.md).

**Decisions**

- **HTTPS even locally.** The session cookie is `Secure` and antiforgery refuses plain HTTP, so the
  container is not published on an HTTP port. A proxy ends TLS in front of it and passes the
  original scheme in `X-Forwarded-Proto`, which is exactly what the Container Apps ingress will do.
  The app trusts those headers only because nothing else can reach the container.
- **Flags, not `IsDevelopment()`**, for migrating and seeding: the container runs as Production and
  still needs both for a demo. A real deployment leaves them off.
- **Npgsql's tracing rather than the EF Core instrumentation package**, which is still a beta. It
  sits one layer lower and so sees every command EF Core sends. SQL text is recorded; parameter
  values are not.
- **Only `/api` requests are traced.** Health probes and static files would bury them.
- **Session keys on a volume** (`DataProtection:KeysPath`): without it every new container signs
  everyone out, because the cookie was encrypted with keys the old container took with it.

**Learned**

- Running the real image found what the dev server hid: Zod probes for `eval` with `new Function`,
  which the Content-Security-Policy blocks, so every page logged a violation. The Vite dev server
  sends no CSP. Fixed by building schemas in Zod's `jitless` mode.
- Putting that setting "first" in `main.tsx` did not work: the bundler evaluates shared chunks
  before the entry module's own code. Making every schema import `z` from one configured module
  turns the ordering into a dependency the bundler must respect.
- A request's trace span ends just *after* the client has its response. Tests that looked
  immediately were flaky; they now wait for the span.
- A new Docker volume takes its ownership from the folder it is mounted over, so the image creates
  `/data` owned by the unprivileged user before switching to it.

## Phase 0 — Walking skeleton

**Built**

- Solution layout with `Domain`, `Application`, `Infrastructure`, `Api` and three test projects;
  references restricted per [ADR 0001](adr/0001-use-clean-architecture.md).
- Strict build in `Directory.Build.props`: nullable, warnings as errors, latest analyzers, code style
  enforced on build.
- `/health` endpoint with an integration test; Vite + React page showing "API status".
- `docker-compose.yml` with PostgreSQL 17; credentials from a git-ignored `.env`.
- GitHub Actions CI (`api` and `web` jobs); `main` protected, requiring both checks and a pull request.
- Pull request template with the clean code checklist.

**Decisions**

- No `public partial class Program { }` for integration tests: .NET 10 makes the generated `Program`
  public, and declaring it triggers analyzer ASP0027.
- `GenerateDocumentationFile` is on so that IDE0005 (unnecessary usings) runs during build; CS1591
  (missing XML comments) is turned off because this is an app, not a library.
- Repository made public: branch protection is not available for private repositories on GitHub's
  free plan. History was checked for secrets first.
- PostgreSQL bound to `127.0.0.1` only, and compose fails fast if `.env` is missing a value.

**Learned**

- `$${VAR}` in compose escapes interpolation, so the variable is read inside the container.
- "Require branches to be up to date" means CI tests exactly what `main` will contain after a merge.
