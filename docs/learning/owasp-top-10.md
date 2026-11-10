# OWASP Top 10 (2025) and QualiTrack

The OWASP Top 10 is the industry's shortlist of the most common and most damaging web application risks. The 2025
edition ([top10.owasp.org/2025](https://top10.owasp.org/2025/0x00_2025-Introduction/)) has two new
categories: **A03 Software Supply Chain Failures**, which widens 2021's "Vulnerable and Outdated Components", and
**A10 Mishandling of Exceptional Conditions**. Server-side request forgery (2021's A10) now sits inside A01.

For each item: what it is in one line, where QualiTrack defends against it, and what is still open. "Open" items
have a phase in `plan.md` where they get closed, or a reason they are accepted.

## A01 Broken Access Control

*Users reach data or actions their role does not allow, e.g. by changing an id in the URL.*

- **Deny by default.** Every API group requires a signed-in user (`RequireAuthorization(Policies.SignedIn)`);
  each command handler then checks the role before it does anything (`RaiseNcr`, `AssignInvestigator`,
  `ListInvestigators`, … return `Error.Forbidden` → 403).
- **Per-record checks, not only per-role checks.** `NcrVisibility` defines which NCRs each role may see. The list
  filters on it in SQL, and the detail and photo queries check the one NCR they load. An NCR the user may not
  see answers **404, not 403**, so ids cannot be probed to learn which ones exist.
- **Ownership rules live in the domain.** Only the assigned investigator can plan a fix, and the investigator
  cannot verify their own work (BR-2…BR-8 in `NonConformance`), whatever the UI shows.
- The web app hides controls a role cannot use (`ProtectedRoute`, `permissions.ts`). That is only for the
  user's convenience. The API repeats every check, and the HTTP integration tests call it directly with the
  wrong role to prove it.
- **SSRF:** does not apply. The API never fetches a URL a user supplied.

## A02 Security Misconfiguration

*Insecure defaults, verbose errors, missing hardening headers, debug features left on.*

- Security headers on every response, errors included (`SecurityHeaders`): a strict CSP
  (`default-src 'self'; frame-ancestors 'none'; object-src 'none'`), `X-Content-Type-Options: nosniff`,
  `Referrer-Policy: no-referrer`.
- OpenAPI, automatic migrations and demo-user seeding run **in Development only** (`Program.cs`).
- Errors are ProblemDetails with no stack traces (`UseExceptionHandler`); a malformed body is a 400 in every
  environment, not a 500.
- Secrets (connection strings, demo passwords) are never in `appsettings*.json`. They come from environment
  variables or user secrets, and `.env` is git-ignored.
- **Open:** no `Strict-Transport-Security` (HSTS) header yet, and `AllowedHosts` is `*`. Both depend on the
  production host name and the TLS termination in front of the app, so they are set with the deployment
  (Phase 8).

## A03 Software Supply Chain Failures

*A compromised or vulnerable dependency, build tool or pipeline.*

- Lockfiles are committed (`package-lock.json`; NuGet restores exact versions) and CI installs with `npm ci`,
  so a build cannot pick up a newer, unreviewed version.
- Dependencies are kept small, and rule 5 of `plan.md` makes every new package an explicit decision.
- At the time of writing, `dotnet list package --vulnerable` and `npm audit --omit=dev` (what ships to
  browsers) report nothing. `npm audit` does flag moderate advisories in `lighthouse`'s telemetry
  dependencies. They are dev-only and run only on a developer's machine against localhost.
- **Open:** nothing in CI checks dependencies automatically. Phase 7 adds Dependabot plus `npm audit` and
  `dotnet list package --vulnerable` steps, and GitHub Actions pinned to commit SHAs.

## A04 Cryptographic Failures

*Sensitive data sent or stored without proper cryptography.*

- Passwords are hashed by ASP.NET Core Identity (PBKDF2 with a per-user salt), never stored or logged.
- The session and antiforgery cookies are `Secure` (HTTPS only) and `HttpOnly`. The API redirects HTTP to HTTPS,
  and the dev server proxies to the HTTPS profile.
- No home-made cryptography: sessions are Identity's data-protected cookies.
- **Open:** HSTS, as under A02. In Azure, database TLS and encryption at rest come with the managed services
  (Phase 8).

## A05 Injection

*Untrusted input interpreted as code: SQL, script (XSS), OS commands.*

- **SQL:** every query goes through EF Core LINQ, which sends parameters. There is no `FromSqlRaw` or
  `ExecuteSqlRaw` anywhere.
- **XSS:** React escapes everything it renders, and there is no `dangerouslySetInnerHTML`. The CSP refuses
  inline and third-party scripts as a second line of defence. The session cookie is `HttpOnly`, so even an
  injected script could not read it.
- **Files:** a photo's type is judged on its first bytes (`PhotoFormat.Recognise`), not on the declared
  `Content-Type`. It is stored under a name the server builds (`NcrPhotoFile.KeyFor`), and `LocalFileStore`
  refuses any key that could leave its folder (`..`, slashes). It is served back with the recognised type and
  `nosniff`, so an HTML file posing as a PNG is never rendered as a page.
- **OS commands:** does not apply. The API never starts a process.

## A06 Insecure Design

*Flaws that no correct implementation can fix, because the design itself never considered the threat.*

- The business rules (who may move an NCR to which state) are designed and tested first, in the domain
  (ADR 0002). The state machine refuses invalid transitions with a 409 whatever the caller does.
- Separation of duties is a designed rule, not a UI convention: the investigator who fixed an NCR cannot
  verify it.
- The cookie-vs-JWT decision was made on threats (XSS token theft, CSRF, revocation): ADR 0006.
- Abuse cases are tests: wrong role → 403, someone else's NCR → 404, missing CSRF token → 400, oversized or
  disguised photo → 400.

## A07 Authentication Failures

*Weak login, credential stuffing, broken session handling.*

- ASP.NET Core Identity rather than home-made authentication. Its password rules apply (length, mixed case,
  digit, symbol).
- **Account lockout** after repeated failures (`CheckPasswordSignInAsync(..., lockoutOnFailure: true)`)
  slows guessing at one account. **Rate limiting** per client address (`LoginRateLimit`, 5 a minute) slows
  trying one password across many accounts.
- Login gives one message for "no such user" and "wrong password", so it does not reveal which emails exist.
- Sessions are server-validated cookies that logout ends. `SameSite=Strict` plus an antiforgery token on every
  write defends against CSRF (`AntiforgeryProtection`).
- **Open:** no multi-factor authentication. That is out of scope for the PRD's internal demo users, and would be
  the next step for a real deployment.

## A08 Software or Data Integrity Failures

*Trusting code or data whose integrity was never checked: unsigned updates, unsafe deserialisation.*

- Request bodies are bound to explicit DTO records with `System.Text.Json`. There is no polymorphic or
  type-name deserialisation, and enums must be their names (numbers are refused).
- Uploaded photos are checked by content and size before they are stored (A05).
- No CDN scripts: every script the web app runs comes from its own build, and the CSP enforces that.
- CI builds and tests every change before it can reach `main` (protected branch, required checks).

## A09 Security Logging & Alerting Failures

*Attacks go unnoticed because nothing is logged, or nobody is told.*

- One structured log entry per request: method, path, status, duration and a correlation ID (`CorrelationId`,
  `AddHttpLogging`). That includes every 401, 403 and 429, so brute-force and probing show up as patterns.
- Headers, query strings and bodies are deliberately **not** logged, because they can hold cookies, passwords
  and personal data.
- **Open:** no alerting yet. Phase 6 adds OpenTelemetry and Phase 8 an uptime alert. A real deployment would
  also alert on spikes of 401/429 responses and on account lockouts.

## A10 Mishandling of Exceptional Conditions

*Errors that leak details, leave state half-changed, or "fail open".*

- **Fail closed:** `NcrVisibility` maps an unknown role to "sees nothing". A missing configuration value
  (connection string, demo passwords) stops startup instead of running with a default.
- Expected failures are `Result` values with a kind (validation, not found, forbidden, conflict) and map to
  one ProblemDetails status in one place (`ResultHttpMapping`, ADR 0005). Unexpected exceptions become a
  bare 500 without details.
- One transaction per command (`IUnitOfWork`), so a failed command leaves no half-saved NCR. A photo that fails
  to store means its NCR change is not saved either.
- A Redis outage degrades to counting from the database instead of failing the dashboard.
- The web app shows a clear message for an unreachable API or an unknown NCR rather than a blank page.
