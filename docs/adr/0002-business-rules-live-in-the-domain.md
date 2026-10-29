# 2. Business rules live in the domain

- **Status:** Accepted
- **Date:** 2026-10-29

## Context

ADR 0001 splits the backend into Domain, Application, Infrastructure and Api. That says where code
*may* go, not where each rule *must* go. With nine commands to build, the easy path is to check
"is the NCR open?" or "is this the assigned investigator?" in the handler, right before calling the
entity. Then the rule exists in two places, or only in the handler, and the entity can be put into
a state the PRD forbids by any caller that skips the handler (a seed script, a test, a future
endpoint).

There are two kinds of check, and they need different homes:

- **Who may call this use case at all**: depends on the caller's role (PRD §4). An Inspector may
  not assign an investigator, whatever NCR it is.
- **Whether this change is allowed on this NCR**: depends on the NCR's state and data. BR-1 to
  BR-9: required fields, the critical due date, only the assigned investigator, verifier is not the
  investigator, finished NCRs are read-only.

## Decision

Every business rule (BR-1 … BR-9) is enforced by the `NonConformance` aggregate or its value
objects, and nowhere else.

- `Status` has a private setter; it changes only through entity methods (`AssignInvestigator`,
  `PlanFix`, `Verify`, …), and every change goes through one `MoveTo` that writes the history
  (BR-7).
- Entity methods check before they change anything, so a failed command leaves the NCR unchanged.
- The domain never reads the clock or the current user. Handlers pass both in as an `AuditStamp`.
- Handlers check only the caller's **role**, then call the entity. They load, stamp, call and save
  (shared in `NcrCommandRunner`); they do not decide.
- A rule is tested once, in `QualiTrack.Domain.Tests`. Handler tests check the wiring: role check,
  not found, that a broken rule comes back as the right error kind, and that nothing is saved.

For example, BR-3 ("only the assigned investigator") is split: `PlanFixHandler` refuses anyone who
is not an Investigator; `NonConformance.PlanFix` refuses an Investigator who is not *this* NCR's.

## Consequences

**Good**

- Each rule has one home, named after the rule, tested in milliseconds without a database.
- No caller can bypass a rule: the only way to change an NCR is through its methods.
- Handlers are short (most are a role check and one line), so adding a use case is cheap.

**Bad**

- The domain must say *which kind* of rule broke, so the API can answer 400, 403 or 409. That is
  ADR 0005.
- Some checks need data the domain does not have (A2: the assignee must have the Investigator role).
  Those stay in the handler, through `IUserDirectory`, and are the documented exception.
- EF Core must work with private setters and backing fields (handled in Infrastructure).

## Alternatives considered

- **Rules in handlers, anaemic entities:** simple to start, but rules scatter across handlers and
  nothing stops a caller from setting `Status` directly.
- **Validation library (e.g. FluentValidation) on commands:** good for request shape (missing
  fields, lengths) at the API edge, but it would duplicate BR-1 and cannot see the NCR's state. May
  still be used for request DTO shape in Phase 3; never for business rules.
