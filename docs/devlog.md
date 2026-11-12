# Devlog

What happened in each phase, what was decided, and what was learned. Newest phase first.

## Phase 8 — Ship and tell: the deployment (2026-11-12)

**Done**

- Tag `v1.0.0` built `ghcr.io/winnie8596/qualitrack:v1.0.0`; `infra/provision.sh` created the
  resource group, PostgreSQL, the file share, the Container Apps environment and the app.
- Live at <https://qualitrack.bravesky-f3694f95.japaneast.azurecontainerapps.io>. Checked: `/health`, the security headers, sign-in as each role, 401 without a
  session, and the Playwright lifecycle test run against the deployed app.
- GitHub can deploy without a stored secret: an app registration trusted for this repository's
  `production` environment (OIDC), Contributor on the one resource group only.
- `APP_URL` set, so the uptime check now runs for real.

**Learned**

- **A script that was never run is a draft.** Three things only running it could show:
  the Azure CLI now keeps `create --database-name` for elastic clusters, so the database is its own
  step; running `env create` twice made a second Log Analytics workspace, so it is now skipped when
  the environment exists; and Azure created the environment in "express" mode, which cannot mount a
  file share (`ExpressEnvironmentResourceNotSupported`).
- The mode is invisible in the CLI and in the stable API: only a preview API version showed
  `environmentMode: Express`. A PATCH to `WorkloadProfiles` converted it, in about twenty minutes.
  This is not in Microsoft's documentation, so the script's handling of it may age.
- The first image push failed with `denied` after every layer had uploaded; the same job passed
  on a retry. A package pushed by a public repository's workflow is public from the start.
- Idempotent steps paid off: a network timeout halfway cost nothing but a second run.

## Phase 8 — Ship and tell (prepared, not deployed)

**Built**

- `cd.yml`: a `v*` tag builds the image, pushes it to GitHub Container Registry and updates the
  Azure Container App, signing in to Azure with GitHub's identity token (OIDC) rather than a secret.
- `uptime.yml`: `/health` every 15 minutes; a failed run is the alert email.
- `infra/provision.sh` and `infra/README.md`: the Azure resources, what they cost and how to delete them.
- The README: pitch, screenshots, lifecycle and architecture diagrams, rules, decisions, how to run
  and test.

**Decisions**

- **Not deployed yet.** The resources cost roughly US$25–30 a month of student credit, so they will
  be created when the demo is needed. The plan's boxes stay open: none of this has run against Azure.
- **GitHub Container Registry, not Azure Container Registry:** free for a public repository, and
  the workflow's own token can push to it.
- **No Redis and one replica in Azure.** The app already caches in memory when no Redis is
  configured; a managed Redis would cost more than the rest together.
- **A script, not Terraform**, for now: Terraform is the plan's stretch task.

**Learned**

- A screenshot is a test. The README's picture of an NCR showed "User 01a1c471" as both the
  inspector and the investigator: .NET's new GUIDs are ordered by time, so users created together
  share their first characters. The audit trail now shows the end of the id.

## Phase 7 — Quality pass

**Built**

- `AnalysisLevel` raised from `latest` to `latest-recommended`; the build is still zero warnings.
- Coverage in CI: coverlet collects per test project, `scripts/check_coverage.py` merges the three
  reports, writes the table to the job summary and fails the `api` job when Domain is below 95 % or
  Application below 85 %. Measured: Domain 98.8 %, Application 99.1 %, Infrastructure 98.4 %,
  Api 96.6 %.
- Every file in `src/` and `web/src/` read against the clean code checklist.

**Analyzer findings, all fixed, none suppressed**

| Rule | Finding | Fix |
| --- | --- | --- |
| CA1716 | The type `Error` is a keyword in other .NET languages | Renamed to `UseCaseError` |
| CA1000 | `Result<T>.Success` / `.Failure` were static members on a generic type | Removed; nothing called them, the implicit conversions build the result |
| CA1725 | `OnModelCreating(modelBuilder)` named its parameter differently from its base | Renamed to `builder` |
| CA1711 | `ApiCollection`, `PostgresCollection` end in "Collection" but are not collections | Renamed to `ApiTestGroup`, `PostgresTestGroup` |

**Smells found and fixed**

| Checklist item | Where | What was wrong | Fix |
| --- | --- | --- | --- |
| ≤ 3 parameters | `DemoData/DemoNcrCatalogue`, `DemoNcrBuilder` | Helpers took 4 to 8 strings in a row; `Build` took 4 arguments | Entries read `Major(title, description, location).Closed(fix)`; the builder holds the cast |
| No dead code | `DemoNcrBuilder.TimeNeededFor` | Written, never called | Deleted |
| Command–query separation | `IFileStore.SaveAsync` | A command that returned the key it had just been given; no caller used it | Returns `Task` |
| Names reveal intent | `IFileStore` | The same thing was `fileName` in one method and `key` in the other | `key` in both |
| No duplicated knowledge | `PhotoFormat` | "5 MB" in the message beside `MaxLength = 5 * 1024 * 1024` | The message is built from the constant |
| No duplicated knowledge | `web`: raise form, NCR page | "5 MB" typed in three messages beside `maxPhotoBytes` | One `maxPhotoSize` |
| No magic numbers | `LocalFileStore`, `NcrNumber` | `81920` twice; year bounds `1000` and `9999` inline | Named constants |
| Comments explain why | `NcrEndpoints`, `SecurityHeaders` | Two comments described the code as it was two phases ago | Rewritten |

**Reviewed and kept, with the reason**

- **Handlers take 3 to 5 constructor dependencies**, and `NcrDetails` and the view records have more
  than three fields. The rule is about a caller having to get a row of arguments in the right
  order; a container fills constructors, and the records *are* the grouping the rule asks for.
- **Each handler repeats `if (currentUser.Role != ...) return Forbidden(...)`.** The shape repeats;
  the knowledge does not: each line is that use case's own rule and message, visible at the top of
  its handler. A shared helper would save one line and hide it.
- **`!` in request DTOs** (`Title!`): each has a comment saying validation has already rejected a
  missing value, which is the exception rule 6 allows.
- **`86400` in the dashboard's SQL**: seconds per day inside a raw query, with the unit in the
  column it produces.
- **Dead code, unused packages, TODOs:** none found beyond `TimeNeededFor`. Every NuGet and npm
  package is referenced; `@testing-library/dom` and the `@types/*` packages are peer and type
  dependencies, not unused.

**Learned**

- Coverage per test project undercounts: the domain looked 90 % covered until the reports were
  merged by class and line and generated code (the regex source generator) was excluded.
- The file with the most checklist findings was the newest one. Reviewing your own latest code with
  the same list as everyone else's is the point of the pass.

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

## Phases 1–5 — in brief

Written up in Phase 7 from the pull requests; the detail is in each PR and in the ADRs.

- **Phase 1, domain (PRs #3–#5).** BR-1 … BR-9 in `QualiTrack.Domain`, test-first, with no
  packages. Time and user arrive as an `AuditStamp` ([ADR 0003](adr/0003-time-and-user-passed-into-the-domain.md)).
- **Phase 2, application (PRs #6, #7).** One handler per command, `Result` at the boundary and
  exceptions inside ([ADR 0005](adr/0005-exceptions-inside-results-at-the-boundary.md)); rules stay
  in the domain ([ADR 0002](adr/0002-business-rules-live-in-the-domain.md)).
- **Phase 3, slice S1 (PR #8).** EF Core on PostgreSQL with actions and history as JSON columns,
  tested against a real database ([ADR 0004](adr/0004-testcontainers-over-in-memory-database.md));
  cookie sessions with antiforgery ([ADR 0006](adr/0006-cookie-auth-over-jwt.md)); the React app
  with Tailwind ([ADR 0007](adr/0007-tailwind-css-for-styling.md)) and React Router
  ([ADR 0008](adr/0008-react-router-for-routing.md)).
- **Phase 4, slices S2–S7 (PRs #9–#14).** List with indexes measured by `EXPLAIN ANALYZE`
  ([notes](learning/sql-indexes.md)), triage, investigator work, verification with a Playwright
  run of the whole lifecycle, the dashboard cached in Redis, and the photo.
- **Phase 5, browser quality (PR #15).** Keyboard-only lifecycle, axe with no WCAG 2.2 AA
  violations, Lighthouse, 375 px layout, and the [OWASP Top 10 notes](learning/owasp-top-10.md).

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
