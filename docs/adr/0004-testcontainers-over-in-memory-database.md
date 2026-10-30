# 4. Testcontainers over an in-memory database

- **Status:** Accepted
- **Date:** 2026-10-29

## Context

Infrastructure code is only correct if it is correct *against PostgreSQL*. PRD §8 asks for every
endpoint to be integration-tested against real PostgreSQL, and the persistence built in slice S1
leans on things only PostgreSQL does:

- **JSON columns.** Corrective actions and status history are stored as JSON arrays on the NCR row
  (EF Core complex collections), so the aggregate loads in one query and the audit trail keeps its
  order (BR-7).
- **Raw SQL.** `NcrNumberGenerator` uses one `INSERT … ON CONFLICT … DO UPDATE … RETURNING`
  statement so concurrent raises queue on the year's row lock and never share a number (BR-9).
- **Constraints and types.** A unique index on the NCR number, `timestamptz` for audit times, and
  migrations applied at startup in Development.

The usual shortcuts each fake one of these away:

- **EF Core InMemory provider** is not a relational database: no SQL, no transactions, no unique
  indexes, no migrations, no JSON columns. `SqlQueryRaw` does not run at all. A test that passes
  there says nothing about production.
- **SQLite** is relational, but a different dialect: different `ON CONFLICT` and JSON behaviour,
  no `timestamptz`, coarser locking. The migrations are generated for Npgsql and would not apply.
- **A shared local or CI database** works, but tests depend on whoever set it up, leak state into
  each other and break when the schema drifts.

## Decision

Tests that touch the database run against **real PostgreSQL in Docker, started by Testcontainers**,
using the same image major version as `docker-compose.yml` (`postgres:17-alpine`).

- `PostgresFixture` starts one container, applies the real migrations, and is shared by every
  persistence test class through an xUnit collection. Startup cost is paid once per collection.
- Tests share that database, so each keeps to its own data: a fresh NCR number per test, a year of
  its own for sequence tests. No test relies on a clean database or on running order.
- Concurrency is tested for real: `Concurrent_raises_get_different_numbers` fires 20 raises, each in
  its own `DbContext`, as separate requests would. It fails against a read-then-write version.
- API integration tests (`QualiTrackApiFactory`) start a second, empty container and point the app
  at it, so the startup migration is tested on a fresh database and every endpoint runs against
  PostgreSQL.
- Domain and Application tests stay database-free (hand-written fakes), so the fast feedback loop
  for business rules is untouched (ADR 0002).

## Consequences

**Good**

- A green test means the mapping, the SQL, the constraints and the migrations work on the database
  production uses.
- Bugs that only show up under concurrency (two raises getting the same number) are caught by a test.
- No setup for contributors or CI beyond Docker; every run starts from a clean, migrated database.

**Bad**

- Docker is required to run the integration tests, locally and in CI. `ubuntu-latest` runners have
  it; on Windows, Docker Desktop must be running.
- Slower: a few seconds to start each container, and database round-trips per test. Kept in check
  by sharing containers per collection and by keeping rules in the fast Domain tests.
- Shared database means tests must be written to not collide (own number, own year). A careless test
  could depend on another's data; review watches for it.

## Alternatives considered

- **EF Core InMemory provider:** fastest, but cannot run the raw SQL, JSON columns, unique index or
  migrations this code depends on. Microsoft's own guidance discourages it for testing EF Core code.
- **SQLite (in-memory mode):** a real database, but the wrong one; the Npgsql migrations and the
  `ON CONFLICT … RETURNING` statement would need a second, SQLite-only version that is never shipped.
- **Repository fakes only:** already used for handler tests, but they test the fake, not the mapping.
- **Shared database in CI (service container):** real PostgreSQL, but ties tests to CI configuration
  and leaves local runs needing a hand-managed database.
