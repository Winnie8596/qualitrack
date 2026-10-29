# 5. Exceptions inside, results at the boundary

- **Status:** Accepted
- **Date:** 2026-10-29

## Context

Use cases fail in expected ways: the NCR does not exist, the caller has the wrong role, a title is
blank, the NCR is already closed. The API must turn each into the right status code (PRD E1,
`plan.md` Conventions): 400 for bad input, 403 for wrong role or not your NCR, 404 for not found,
409 for a change the current status does not allow.

Two styles are common:

- **Exceptions everywhere:** throw, and let middleware map exception types to status codes. Callers
  cannot see from a method's signature that it can fail, and control flow hides in `catch` blocks.
- **Results everywhere:** every method returns success or an error. Explicit, but in the domain it
  turns every entity method and value-object constructor into `Result` plumbing, and a forgotten
  check silently continues.

The domain also cannot tell the handler enough on its own: a single `DomainException` type made
"NCR is not Open" (409) and "critical NCR due after day 7" (A2: a validation error, 400)
indistinguishable.

## Decision

- **Inside the domain, broken rules throw.** `DomainException` carries a `DomainErrorKind`, set at
  the throw site by the only code that knows which rule broke: `InvalidInput` (a supplied value is
  not acceptable), `InvalidState` (not allowed in the NCR's current status), `NotPermitted` (this
  user may not do this to this NCR, BR-3/BR-5). A constructor or method either succeeds or leaves
  nothing changed.
- **At the handler boundary, exceptions become results.** Handlers return `Result` / `Result<T>`
  with an `Error` of kind `Validation`, `NotFound`, `Forbidden` or `Conflict`. They never throw for
  an expected failure.
- **One mapping, one place.** `DomainErrors.From` maps `InvalidInput` → `Validation`,
  `InvalidState` → `Conflict`, `NotPermitted` → `Forbidden`. `NcrCommandRunner` is the only place
  that catches `DomainException` for commands on an existing NCR; `RaiseNcr` uses the same mapping.
- **The API maps `ErrorKind` to HTTP** (Phase 3): `Validation` → 400, `Forbidden` → 403,
  `NotFound` → 404, `Conflict` → 409, as ProblemDetails. It never inspects messages.
- Anything else (a bug, the database is down) is not caught by handlers and becomes a 500.

## Consequences

**Good**

- Domain code reads as plain rules; each guard is one line.
- A handler's signature (`Task<Result>`) says it can fail, and callers must handle it.
- The status code follows from the rule's own kind, so A1/A2 get field errors and BR-3 gives 403
  without any handler knowing the rules.

**Bad**

- Two error vocabularies (`DomainErrorKind`, `ErrorKind`) and a mapping between them. Accepted: the
  domain must not reference Application, and the mapping is one switch expression with a test.
- Exceptions cost more than returns. Irrelevant at this scale: a broken rule is one per request.
- A new throw site must pick the right kind. Domain tests assert the kind for every rule.

## Alternatives considered

- **Exceptions to the API, middleware maps them:** less code in handlers, but use-case failures
  become invisible in signatures, and handler tests must assert on exceptions.
- **Results in the domain too:** fully explicit, but noisy, and composing value-object
  construction becomes `Result` chaining with no gain at this size.
- **Handler pre-checks state to pick the error kind:** duplicates the domain's rules (ADR 0002).
