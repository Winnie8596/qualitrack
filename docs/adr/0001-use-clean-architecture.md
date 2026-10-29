# 1. Use clean architecture

- **Status:** Accepted
- **Date:** 2026-10-29

## Context

QualiTrack's value is in its business rules (BR-1 … BR-9 in `PRD.md`): who may move an NCR between
states, when, and with what evidence. Those rules must be easy to find, easy to test, and impossible
to bypass from a controller, a database script or a future UI.

In a typical layered ASP.NET app the rules drift into controllers and EF Core entities, so testing a
rule needs a web host or a database, and a change to the database or framework touches the rules.

This is also a portfolio project: the owner must be able to point at exactly where each rule lives
and explain why it lives there.

## Decision

Split the backend into four projects with dependencies that point inwards only:

```
QualiTrack.Api ─────────────┐
   │                        ▼
   └──▶ QualiTrack.Infrastructure ──▶ QualiTrack.Application ──▶ QualiTrack.Domain
```

| Project | Holds | May reference |
| --- | --- | --- |
| `Domain` | Entities, value objects, business rules, `DomainException` | Nothing (no NuGet, no project) |
| `Application` | Use-case handlers, `Result`, interfaces for what it needs from the outside (`INcrRepository`, `IClock`, `ICurrentUser`, …) | `Domain` |
| `Infrastructure` | EF Core, PostgreSQL, file storage, the system clock: implementations of the Application interfaces | `Application` |
| `Api` | HTTP endpoints, auth, wiring everything together (composition root) | `Application`, `Infrastructure` |

Business rules live only in `Domain`. `Application` orchestrates (load, call the entity, save) but
does not decide. Endpoints are thin: map the request to a command, run the handler, map the result
to HTTP.

The project references above are the only ones allowed. Today this is checked in review (see the PR
template); an automated architecture test can be added later if the rule is ever broken.

## Consequences

**Good**

- Every business rule is unit-testable with plain xUnit: no web host, no database, tests run in
  milliseconds.
- Swapping PostgreSQL, the file store or the auth scheme touches `Infrastructure`/`Api` only.
- One obvious place to look for each kind of code, which helps reviewers and coding agents alike.

**Bad**

- More projects and more mapping code (entities ↔ DTOs, `Result` ↔ HTTP) than a single-project app.
- EF Core must map private fields and value objects without the Domain knowing about EF, which needs
  extra configuration in `Infrastructure`.
- For an app this size it is arguably more structure than strictly needed; the trade-off is accepted
  because demonstrating it is a goal (PRD G5).

## Alternatives considered

- **Single project, folders by layer:** less ceremony, but nothing stops a controller from calling
  `DbContext` directly or the domain from reading `DateTime.Now`.
- **Vertical slices:** organises by feature, which scales well, but spreads rules across slices. A
  good fit later inside `Application`; not a replacement for keeping rules in `Domain`.
