# 3. Time and user are passed into the domain

- **Status:** Accepted
- **Date:** 2026-11-10 (the decision dates from Phase 1; written up in Phase 6)

## Context

Almost every business rule in QualiTrack depends on *when* something happens or *who* does it:

- BR-2: a critical NCR must be due within 7 days of being raised.
- BR-3: only the assigned investigator may plan the fix, add and complete actions, and submit.
- BR-5: the investigator cannot verify their own fix.
- BR-7: every status change records who made it and when.
- BR-8: an unfinished NCR is overdue once its due date has passed.

The usual way to get those two facts is to reach for them where they are needed:
`DateTimeOffset.UtcNow` for the time, and the signed-in user from `HttpContext` or a static
"current user" for the person. Inside an entity that has three costs:

- **Tests cannot control them.** "Critical NCR can be due on day 7" needs a clock that says exactly
  what the test wants. With `UtcNow` in the entity, the test either sleeps, tolerates a margin, or
  fails at midnight.
- **The domain would depend on the web.** Reading the user from `HttpContext` pulls ASP.NET Core
  into `QualiTrack.Domain`, which must reference nothing (ADR 0001).
- **A method's inputs would be hidden.** `ncr.Verify()` would silently depend on two ambient values
  that its signature does not mention.

## Decision

- **The domain never reads the clock or the current user.** No `DateTime.Now`,
  `DateTimeOffset.UtcNow`, `TimeProvider`, `HttpContext` or static accessor in `QualiTrack.Domain`.
- **Both arrive together as an `AuditStamp(UserId By, DateTimeOffset At)`**, a value object passed
  to every method that changes an NCR: `Raise(number, details, raised)`,
  `AssignInvestigator(investigator, dueDate, stamp)`, `Verify(stamp)` and so on. The stamp rejects a
  time that is not UTC.
- **Questions about time take the time as a parameter:** `IsOverdue(now)`, and
  `OverdueAt(now)` for the same rule as a database filter.
- **The Application layer owns the ports** `IClock` and `ICurrentUser`. A handler reads each once,
  builds the stamp, and passes it down. Infrastructure implements `IClock` with the system clock;
  the API implements `ICurrentUser` from the session cookie's claims.

```csharp
// Handler (Application): the only place the two ambient facts are read
var stamp = new AuditStamp(currentUser.Id, clock.UtcNow);
ncr.Verify(stamp);

// Entity (Domain): a pure function of its state and its arguments
public void Verify(AuditStamp stamp)
{
    EnsureStatus(NcrStatus.PendingVerification);
    EnsureIsNotInvestigator(stamp.By);   // BR-5
    MoveTo(NcrStatus.Closed, stamp);     // BR-7
}
```

## Consequences

**Good**

- Domain tests are plain and exact. `Ncrs.Day(3, Investigator)` is a stamp three days after the NCR
  was raised; 126 domain tests run in a fraction of a second with no clock, no fakes and no waiting.
- One reading of the clock per use case. Everything a handler does carries the same instant, so an
  NCR cannot be "assigned" a few microseconds before the due-date check that allowed it.
- The audit trail cannot be forgotten: there is no way to change status without supplying a stamp,
  because `MoveTo` requires one.
- History can be written honestly after the fact. The demo seed (`DemoData/`) builds fifty NCRs
  through the same entity methods with stamps in the past, so seeded data obeys every rule and has
  a real history. With `UtcNow` inside the entity that would have needed a back door.

**Bad**

- Every state-changing method has one more parameter, and every handler has two more dependencies.
  Accepted: it is the same line in each handler, and the parameter is what makes the rule testable.
- Nothing in the compiler stops someone writing `DateTimeOffset.UtcNow` in the domain later. The
  guard is review plus the rule in `plan.md`; a banned-symbol analyzer would enforce it but adds a
  package, which was not worth it at this size.
- The stamp is trusted. The domain cannot tell whether the handler passed the real user; that is
  the handler's job, and handler tests cover it with a fake `ICurrentUser`.

## Alternatives considered

- **`TimeProvider` injected into entities:** the framework's own testable clock. It still makes the
  entity hold a service, which EF Core cannot supply when it loads one from the database, and it
  does nothing for the user.
- **A static `Clock.UtcNow` that tests can replace:** small to write, but it is shared mutable
  state: tests running in parallel would set each other's time.
- **Stamping in the database (`DEFAULT now()`, triggers) or in `SaveChanges`:** fine for
  "last modified" columns, but BR-2 and BR-8 need the time *during* the decision, before anything is
  saved, and BR-3 and BR-5 need the user there too.
