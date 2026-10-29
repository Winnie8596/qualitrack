# QualiTrack — Implementation Plan (for coding agents)

> Read `PRD.md` first. This file is the order of work and the rules of engagement.
> Work **one phase at a time**. Do not start a phase until the previous phase's "Done when" checks all pass.

---

## 0. Rules for agents

### Working agreement

1. **One task at a time.** Pick the next unchecked task in the current phase, do it, verify it, tick it (`- [x]`), then stop and summarise.
2. **Tests first.** For any behaviour: write the failing test, show it fails, implement the minimum to pass, refactor. Never write production code for a rule without a test.
3. **Small diffs.** One task = one commit using Conventional Commits (`feat(domain): ...`, `test(api): ...`, `refactor: ...`, `ci: ...`, `docs: ...`).
4. **Explain.** After each task, write 3–5 lines: what changed, why, which principle it applies, anything the owner should learn or check. The owner must be able to defend this code in an interview.
5. **Ask, don't guess.** If `PRD.md` is ambiguous, or a task needs a new package, a schema change outside the task, or a change to a business rule, stop and ask.
6. **Never** weaken a test to make it pass, disable an analyzer rule, add `#pragma warning disable`, suppress nullable warnings with `!` without a comment explaining why, or commit secrets.
7. Update `docs/devlog.md` at the end of each phase.

### Verify before saying "done"

```bash
dotnet build --configuration Release      # must have 0 warnings, 0 errors
dotnet test --configuration Release       # all green
dotnet format --verify-no-changes         # formatter clean
cd web && npm run lint && npm run build   # once web exists
```

### Architecture rules (hard)

```
src/QualiTrack.Domain          → references nothing (no NuGet, no other project)
src/QualiTrack.Application     → references Domain only
src/QualiTrack.Infrastructure  → references Application (+ EF Core, Npgsql)
src/QualiTrack.Api             → references Application + Infrastructure (composition root)
```

- Business rules live **only** in Domain entities/value objects. Application orchestrates; it does not decide.
- `NonConformance.Status` has a private setter. Status changes only via entity methods.
- Domain never calls `DateTime.Now`, `DateTimeOffset.UtcNow`, or reads users from context. Time and user are passed in (`AuditStamp`).
- Infrastructure implements interfaces declared in Application (`INcrRepository`, `IUnitOfWork`, `IClock`, `ICurrentUser`, `IFileStore`, `INcrNumberGenerator`).
- Api endpoints are thin: map request → command/query → handler → map result to HTTP.

### Clean code checklist (every diff)

- Names reveal intent; no abbreviations; booleans read as questions (`IsOverdue`)
- Functions do one thing; ≤ 3 parameters (group related ones into a record)
- Command–query separation: commands return `void`/`Result`, queries change nothing
- No flag arguments; no `null` returns where an empty collection or `Result` fits
- No duplicated knowledge (rules, constants, formats live in one place)
- No commented-out code; comments explain *why*, not *what*
- Tests: Arrange / Act / Assert separated by blank lines; names describe behaviour (`Investigator_cannot_verify_their_own_fix`)

### Conventions

- C#: file-scoped namespaces, `sealed` by default, primary constructors where they help, records for value objects and DTOs, `async` methods end with `Async` and take a `CancellationToken`.
- API routes: `/api/ncrs`, `/api/ncrs/{id}`, `/api/ncrs/{id}/assign`, `/plan-fix`, `/actions`, `/actions/{actionId}/complete`, `/submit`, `/verify`, `/reject`, `/cancel`, `/api/dashboard`, `/api/auth/login`, `/api/auth/logout`, `/api/auth/me`.
- Errors: ProblemDetails. `DomainException` → `409 Conflict`; validation → `400`; not found → `404`; wrong role or not your NCR → `403`.
- TypeScript: strict mode, no `any`, one component per file, API types in `web/src/api/`.

---

## Full-stack learning map

Source: roadmap.sh/full-stack (2026). The roadmap's reference stack is Node.js + AWS. QualiTrack keeps .NET + Azure (fixed in `PRD.md` §9). Each roadmap topic is covered by its QualiTrack equivalent, and the concepts transfer to any stack.

> Each roadmap topic, where QualiTrack practises it, and what the owner must be able to explain afterwards. When a task touches a topic, the agent's explanation must cover the "Explain it" column.

| Roadmap topic | QualiTrack equivalent | Phase | Explain it (owner, in an interview) |
| --- | --- | --- | --- |
| How the web works: HTTP, DNS, browsers | API requests, cookies, CORS, deployment domain | 0, 4, 8 | What happens from typing the URL to seeing the NCR list; status codes; headers; HTTPS |
| HTML | Semantic markup for forms, tables, nav | 5 | Why `<button>` not `<div onClick>`; labels; landmarks |
| CSS | Layout with Flexbox/Grid, responsive pages | 5 | Box model, specificity, mobile-first, media queries |
| Tailwind CSS | Utility-first styling for the web app | 5 | Trade-offs vs plain CSS / component libraries |
| JavaScript | Async/await, fetch, modules, closures in React code | 0, 5 | Event loop, promises, `this`, equality, array methods |
| TypeScript | Strict typed web app and API types | 0, 5 | Union types, generics, narrowing, why no `any` |
| npm | `web/` dependencies, scripts, lockfile | 0, 5 | `npm ci` vs `npm install`, semver, lockfiles |
| Git | Small commits, branches, rebase, `.gitattributes` | all | Merge vs rebase, resolving conflicts, reverting a bad commit |
| GitHub | PRs, issues, protected `main`, Actions | all | Pull request workflow and code review etiquette |
| React | Components, hooks, routing, forms, server state | 5 | State vs props, effects, re-renders, keys, controlled inputs |
| Node.js (backend) | ASP.NET Core Web API (same role) | 2–4 | Request pipeline, middleware, dependency injection |
| RESTful APIs | `/api/ncrs` resources, ProblemDetails, paging | 4 | Resource naming, verbs, idempotency, status codes, pagination |
| Authentication (JWT) | Cookie auth chosen; JWT compared in ADR 0006 | 4 | Session cookie vs JWT, CSRF, XSS, where tokens live |
| PostgreSQL / SQL | EF Core + migrations + indexes | 3 | Joins, indexes, `EXPLAIN ANALYZE`, transactions, N+1 queries |
| Redis | Cache dashboard numbers via `IDistributedCache` | 6 | Cache-aside, TTL, invalidation, when not to cache |
| Linux basics | CI runners, Docker images, container shell | 0, 6, 8 | Files, permissions, processes, env vars, logs, SSH |
| Docker | Dockerfile, compose, Testcontainers | 0, 3, 6 | Image vs container, layers, volumes, networks, multi-stage builds |
| GitHub Actions (CI/CD) | Build, test, coverage, deploy workflows | 0, 7, 8 | Pipelines, caching, secrets, deploy on tag |
| Cloud (AWS on roadmap) | Azure Container Apps + PostgreSQL + Blob Storage | 8 | Map: EC2/ECS ↔ Container Apps, S3 ↔ Blob, RDS ↔ Azure PostgreSQL, SES ↔ email service |
| Infrastructure as Code (Terraform) | Terraform for the Azure resources (stretch) | 8 | State, plan vs apply, why IaC beats clicking in the portal |
| Configuration management (Ansible) | Not used; container images replace it | — | Why containers reduce the need for server configuration tools |
| Monitoring (Monit on roadmap) | Health checks, structured logs, OpenTelemetry, uptime check | 6, 8 | Logs vs metrics vs traces; what to alert on |

Also essential for interviews though not a node on the roadmap: web security (OWASP Top 10), testing pyramid, accessibility, and frontend performance (Lighthouse). They are covered by tasks in the phases below.

---

## Phase 0 — Walking skeleton

Goal: browser → API → CI, end to end, with no features.

- [x] Create repo layout per `PRD.md` §9 and the tree below; `.gitignore`, `.editorconfig`, `QualiTrack.slnx`
- [x] Create projects: `QualiTrack.Domain`, `.Application`, `.Infrastructure`, `.Api`, `tests/QualiTrack.Domain.Tests`, `tests/QualiTrack.Application.Tests`, `tests/QualiTrack.Api.IntegrationTests`; delete template sample files
- [x] Add project references exactly as in "Architecture rules"
- [x] `Directory.Build.props`: `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, `AnalysisLevel=latest`, `EnforceCodeStyleInBuild=true`
- [x] Api: `/health` via `AddHealthChecks`, OpenAPI in Development (no `public partial class Program { }`: .NET 10 makes `Program` public for `WebApplicationFactory`; ASP0027)
- [x] Integration test: `Health_returns_200_when_the_app_starts`
- [x] `web/`: Vite React TS app showing "API status: healthy | unreachable" from `/health` via dev proxy
- [x] `docker-compose.yml` with PostgreSQL 17 (named volume, credentials from `.env`, `.env.example` committed)
- [ ] `.github/workflows/ci.yml`: jobs `api` (restore, build, test) and `web` (npm ci, lint, build)
- [ ] `docs/adr/0001-use-clean-architecture.md`, `docs/devlog.md`, minimal `README.md`
- [x] `.gitattributes` (LF endings, binary images)
- [ ] Branch protection on `main` requiring green CI; PR template with the clean code checklist
- [ ] `docs/learning/http-basics.md`: owner's own notes on one request traced in browser DevTools (Network tab: method, status, headers, timing)

```
qualitrack/
├─ src/{QualiTrack.Domain, QualiTrack.Application, QualiTrack.Infrastructure, QualiTrack.Api}/
├─ tests/{QualiTrack.Domain.Tests, QualiTrack.Application.Tests, QualiTrack.Api.IntegrationTests}/
├─ web/
├─ docs/adr/ · docs/devlog.md
├─ .github/workflows/ci.yml
├─ docker-compose.yml · .env.example · Directory.Build.props · QualiTrack.slnx · README.md
```

**Done when:** CI green on both jobs; `main` requires green CI to merge; page shows "healthy" locally.

---

## Phase 1 — Domain model (TDD)

Goal: BR-1 … BR-9 enforced and unit-tested in `QualiTrack.Domain`. No packages.

- [ ] Value objects: `DomainException`, `Severity`, `NcrStatus`, `UserId` (record struct), `AuditStamp(UserId By, DateTimeOffset At)`, `NcrNumber.From(year, sequence)`, `NcrDetails(title, description, location, severity)` with validation
- [ ] `CorrectiveAction` (internal `Complete`), `StatusChange(From, To, Stamp, Note)`
- [ ] `NonConformance` aggregate: `Raise`, `AssignInvestigator`, `PlanFix`, `AddCorrectiveAction`, `CompleteAction`, `SubmitForVerification`, `Verify`, `RejectVerification`, `Cancel`, `IsOverdue(now)`, `IsFinished`
- [ ] Test helper `Ncrs` that builds an NCR in each state
- [ ] Tests (one cycle each, commit after each):
  - [ ] `A_new_ncr_starts_open`
  - [ ] `Title_is_required` (theory: `""`, `"   "`) and same for description, location
  - [ ] `Ncr_number_is_padded_to_four_digits`
  - [ ] `Critical_ncr_cannot_be_due_more_than_7_days_after_it_was_raised` / `Critical_ncr_can_be_due_on_day_7`
  - [ ] `Due_date_must_be_in_the_future`
  - [ ] `Only_the_assigned_investigator_can_plan_the_fix`
  - [ ] `Cannot_submit_while_a_corrective_action_is_incomplete`
  - [ ] `Investigator_cannot_verify_their_own_fix` / `Another_user_verifying_closes_the_ncr`
  - [ ] `Rejected_verification_returns_ncr_to_action_in_progress_with_reason`
  - [ ] `A_closed_ncr_cannot_be_cancelled` and `A_cancelled_ncr_cannot_be_assigned`
  - [ ] `Each_status_change_is_recorded_with_who_and_when`
  - [ ] `Ncr_is_overdue_after_its_due_date_unless_finished` (theory)
  - [ ] `A_failed_command_leaves_the_ncr_unchanged`

**Done when:** all tests pass in < 5 s; Domain `.csproj` has no `PackageReference` or `ProjectReference`.

---

## Phase 2 — Application layer

Goal: use cases as small handlers; no framework dependencies beyond `Microsoft.Extensions.*` abstractions.

- [ ] `Result` / `Result<T>` with error kinds: `Validation`, `NotFound`, `Forbidden`, `Conflict`
- [ ] Interfaces: `INcrRepository`, `IUnitOfWork`, `IClock`, `ICurrentUser` (Id + Role), `INcrNumberGenerator`, `IFileStore`
- [ ] Commands + handlers (one file each): `RaiseNcr`, `AssignInvestigator`, `PlanFix`, `AddCorrectiveAction`, `CompleteCorrectiveAction`, `SubmitForVerification`, `VerifyNcr`, `RejectVerification`, `CancelNcr`
- [ ] Queries + handlers: `GetNcrById`, `ListNcrs` (filters, sort, paging, role visibility per PRD D1), `GetDashboard`
- [ ] Role checks in handlers (who may call) — domain still enforces BR-3/BR-5 (who may act on *this* NCR)
- [ ] Map `DomainException` → `Result` `Conflict` at the handler boundary only
- [ ] Unit tests with hand-written in-memory fakes (no mocking library) for every handler: success, not found, forbidden, conflict
- [ ] `docs/adr/0002-business-rules-live-in-the-domain.md`, `0005-exceptions-inside-results-at-the-boundary.md`

**Done when:** every handler has success + failure tests; Application references only Domain and `Microsoft.Extensions.DependencyInjection.Abstractions`.

---

## Phase 3 — Persistence

- [ ] `QualiTrackDbContext` with EF Core + Npgsql; configurations in separate `IEntityTypeConfiguration<T>` classes
- [ ] Map value objects (`UserId`, `NcrNumber`, `NcrDetails`) via conversions/owned types; private backing fields for actions and history; add private parameterless constructors only where EF requires them
- [ ] Unique index on NCR number; indexes on `Status`, `DueDate`, `InvestigatorId`, `RaisedBy`
- [ ] `NcrNumberGenerator` safe under concurrency (per-year sequence table updated in a transaction)
- [ ] `NcrRepository`, `UnitOfWork`, `SystemClock`, `LocalFileStore`
- [ ] Initial migration; apply automatically in Development only
- [ ] Integration tests with Testcontainers PostgreSQL: save/load round-trip preserves actions and history; two concurrent raises get different numbers
- [ ] `docs/adr/0004-testcontainers-over-in-memory-database.md`
- [ ] Run `EXPLAIN ANALYZE` on the NCR list query with 10,000 seeded rows before and after indexes; record results in `docs/learning/sql-indexes.md`
- [ ] Integration test proving the list query issues a single SQL query (no N+1)

**Done when:** persistence tests pass in CI (Docker available on `ubuntu-latest`).

---

## Phase 4 — API and authentication

- [ ] ASP.NET Core Identity on PostgreSQL; cookie auth (`HttpOnly`, `Secure`, `SameSite=Strict`); return `401/403` instead of redirects for `/api/*`
- [ ] Seed three roles and one demo user per role (Development only; passwords from config)
- [ ] `ICurrentUser` implementation from `HttpContext`
- [ ] Endpoints per Conventions, grouped with `MapGroup`, each with `.RequireAuthorization(policy)`
- [ ] Request DTOs validated; responses are DTOs, never entities
- [ ] Global ProblemDetails + `Result` → HTTP mapping in one place
- [ ] Correlation ID middleware + structured request logging
- [ ] Integration tests per endpoint: happy path, 400, 401, 403, 404, 409; full lifecycle test Open → Closed via HTTP
- [ ] `docs/adr/0006-cookie-auth-over-jwt.md`
- [ ] Antiforgery (CSRF) protection for cookie-authenticated state-changing endpoints; test that a request without the token is rejected
- [ ] Security headers (CSP, `X-Content-Type-Options`, `Referrer-Policy`); rate limiting on `/api/auth/login`
- [ ] `docs/learning/owasp-top-10.md`: each OWASP Top 10 item, and where QualiTrack defends against it (or why it doesn't apply)
- [ ] ADR 0006 must compare cookie sessions with JWT (storage, CSRF/XSS exposure, revocation)

**Done when:** full lifecycle integration test passes; OpenAPI document lists every endpoint; writes without an antiforgery token are rejected; security headers are present on every response; login is rate-limited.

---

## Phase 5 — Frontend

- [ ] Routing, auth context from `/api/auth/me`, protected routes per role
- [ ] Typed API client (`web/src/api/`) using `fetch` + TanStack Query; ProblemDetails surfaced as form/field errors
- [ ] Pages: Login · NCR list (filters, sort, paging, overdue badge) · NCR detail (actions shown only when allowed, history timeline) · Raise NCR form (React Hook Form + Zod, photo upload) · Dashboard
- [ ] Status and severity shown with text + colour (never colour only)
- [ ] Component tests for the raise form and status badge (Vitest + Testing Library)
- [ ] One Playwright smoke test: login as each role and run the lifecycle
- [ ] Tailwind CSS for styling (ADR 0007 records the choice); responsive layout tested at 375 px and 1280 px
- [ ] Semantic HTML and accessibility: every input labelled, keyboard-only lifecycle run, axe/Lighthouse accessibility score ≥ 90
- [ ] Lighthouse performance score ≥ 90 on the NCR list page; route-level code splitting

**Done when:** a reviewer can run the whole lifecycle in the browser with the three demo users; Lighthouse performance and accessibility are ≥ 90 on the NCR list page; layout works at 375 px.

---

## Phase 6 — Cross-cutting

- [ ] Configuration per environment (`appsettings.*.json` + environment variables); `.env.example` complete
- [ ] Dashboard queries tested with seeded data
- [ ] Seed script with ~50 realistic NCRs across states for the demo
- [ ] Multi-stage `Dockerfile` for Api (serves built `web/` as static files) and full `docker compose up` run
- [ ] `docs/adr/0003-time-and-user-passed-into-the-domain.md`
- [ ] Redis in `docker-compose.yml`; cache dashboard results with `IDistributedCache` (cache-aside, 60 s TTL, invalidated when an NCR changes status); falls back to the database if Redis is down
- [ ] OpenTelemetry tracing and metrics for API + EF Core; request duration visible locally

**Done when:** `docker compose up` starts a working app from a clean clone; the dashboard still loads with Redis stopped; API traces and request durations are visible locally.

---

## Phase 7 — Quality pass

- [ ] Raise `AnalysisLevel` to `latest-recommended`; fix (don't suppress) findings
- [ ] Coverage report in CI (coverlet); Domain ≥ 95 % line coverage, Application ≥ 85 %
- [ ] Review every file against the clean code checklist; list smells found and fixed in `docs/devlog.md`
- [ ] Remove dead code, unused packages, TODOs without issues

**Done when:** zero warnings at the higher level; coverage thresholds enforced in CI.

---

## Phase 8 — Ship and tell

- [ ] Deploy to Azure Container Apps with managed PostgreSQL; secrets in app settings, not the repo
- [ ] CD workflow on tag `v*`
- [ ] README per the guide: pitch, live link, CI badge, demo GIF/video, problem, state diagram, rules, architecture, ADR links, how to run, testing, what's next
- [ ] Tag `v1.0.0` with release notes
- [ ] (Stretch) Terraform for all Azure resources in `infra/`; `terraform plan` runs in CI on pull requests
- [ ] Uptime check on `/health` with an alert email
- [ ] `docs/learning/cloud-mapping.md`: owner's notes mapping each Azure resource used to its AWS equivalent

**Done when:** PRD §10 success criteria are all met; the `/health` uptime alert is live. The Terraform stretch task is not required.
