# Devlog

What happened in each phase, what was decided, and what was learned. Newest phase first.

## Phase 0 — Walking skeleton (in progress)

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
