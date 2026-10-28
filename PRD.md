# QualiTrack — Product Requirements Document

**Version:** 1.0 · **Owner:** Winnie Ngu Pei Sing · **Status:** Approved for build

> For coding agents: this file defines **what** to build and **why**. `plan.md` defines **how** and **in what order**. If the two disagree, stop and ask.

---

## 1. Summary

QualiTrack is a web app for tracking **non-conformance reports (NCRs)** and their **corrective actions (CAPA)** in a small Malaysian manufacturer or contractor. Inspectors report defects, a QA Manager assigns an investigator, the investigator records the root cause and fixes, and a different person verifies the fix before the NCR is closed. Every change is audited.

It is a portfolio project. Code quality, test coverage of business rules, and clear design decisions matter as much as features.

## 2. Problem

In many small companies NCRs live in Excel sheets, WhatsApp photos and paper forms. As a result:

- NCRs get lost or closed without the fix being checked
- Nobody can see which NCRs are overdue
- There is no reliable record of who did what and when, which hurts audits (e.g. ISO 9001)

## 3. Goals and non-goals

### Goals (v1)

| ID | Goal |
| --- | --- |
| G1 | Take an NCR from report to verified close in one system |
| G2 | Enforce the business rules in section 6 so invalid states cannot happen |
| G3 | Keep a complete, read-only audit trail per NCR |
| G4 | Show managers what is open, critical and overdue at a glance |
| G5 | Demonstrate clean code, clean architecture and automated testing |

### Non-goals (v1)

- Email / push notifications
- Mobile app or offline mode
- Multi-tenancy (one company per deployment)
- AI features
- File storage beyond one optional photo per NCR
- Reporting exports (PDF/Excel)

## 4. Users and roles

| Role | Description | Permissions |
| --- | --- | --- |
| Inspector | Finds and reports defects on site | Raise NCRs; view NCRs they raised |
| Investigator | Supervisor or engineer who fixes the problem | View assigned NCRs; record root cause; add, complete corrective actions; submit for verification |
| QA Manager | Owns quality; assigns and verifies | View all NCRs; assign investigator and due date; verify or reject; cancel; view dashboard |

A user has exactly one role in v1. Roles are seeded; there is no self-registration.

## 5. NCR lifecycle

```
Open ──assign──▶ Investigating ──plan fix──▶ ActionInProgress ──submit──▶ PendingVerification ──verify──▶ Closed
 │                                               ▲                                  │
 └──cancel──▶ Cancelled                          └──────── reject (with reason) ────┘
```

- `Closed` and `Cancelled` are final and read-only.
- Status changes **only** through methods on the `NonConformance` domain entity.

## 6. Business rules

Each rule must have at least one unit test named after the behaviour.

| ID | Rule |
| --- | --- |
| BR-1 | An NCR requires title, description, location, severity (`Minor`, `Major`, `Critical`) and the user who raised it. Blank or whitespace text is rejected. |
| BR-2 | A `Critical` NCR must have a due date no later than 7 days after it was raised. Any due date must be in the future when assigned. |
| BR-3 | Only the assigned investigator can record root cause, add corrective actions, complete them and submit for verification. |
| BR-4 | An NCR can be submitted for verification only when it has a root cause and every corrective action is completed (at least one action must exist). |
| BR-5 | The verifier (verify or reject) must be a different user from the investigator. |
| BR-6 | A `Closed` or `Cancelled` NCR cannot be changed in any way. |
| BR-7 | Every status change is recorded with from-status, to-status, user, timestamp and optional note. Reject and cancel require a reason. |
| BR-8 | An NCR is overdue when its due date has passed and it is not `Closed` or `Cancelled`. |
| BR-9 | NCR numbers are unique, sequential per year, formatted `NCR-YYYY-NNNN` (e.g. `NCR-2026-0001`). |

## 7. User stories and acceptance criteria

### Epic A — Raise and triage

**A1. Raise an NCR** — As an Inspector, I want to report a defect so it gets fixed.
- Given valid details, when I submit, then an NCR is created with status `Open`, a number per BR-9, and me as `RaisedBy`.
- Given a blank title, when I submit, then I see a field error and nothing is saved.
- I can attach one optional photo (JPEG/PNG, max 5 MB).

**A2. Assign an investigator** — As a QA Manager, I want to assign an investigator and due date.
- Given an `Open` NCR, when I assign a user with the Investigator role and a valid due date, then status becomes `Investigating`.
- Given a `Critical` NCR and a due date more than 7 days after it was raised, then I get a validation error (BR-2).

**A3. Cancel an NCR** — As a QA Manager, I want to cancel an NCR raised by mistake.
- Only `Open` NCRs can be cancelled; a reason is required.

### Epic B — Investigate and fix

**B1. Plan the fix** — As the assigned Investigator, I record a root cause and one or more corrective actions.
- Status becomes `ActionInProgress`.
- Any other user gets `403 Forbidden` (BR-3).

**B2. Complete corrective actions** — I mark each action complete; completion time is stored.
- I can add more actions while `ActionInProgress` (e.g. after a rejected verification).

**B3. Submit for verification** — Allowed only per BR-4; status becomes `PendingVerification`.

### Epic C — Verify and close

**C1. Verify** — As a QA Manager (not the investigator), I verify the fix; status becomes `Closed`.
**C2. Reject** — I reject with a reason; status returns to `ActionInProgress`.

### Epic D — Visibility

**D1. NCR list** — Filter by status, severity and overdue; sort by raised date or due date; page size 20. Inspectors see only their own; Investigators see raised-by-them or assigned-to-them; QA Managers see all.
**D2. NCR detail** — Shows details, photo, investigator, due date, root cause, actions and full history (BR-7).
**D3. Dashboard (QA Manager)** — Open NCRs by severity, overdue count, average days from raised to closed (last 90 days).

### Epic E — Access

**E1. Login / logout** with email and password. Unauthenticated API calls return `401`; forbidden actions return `403`.

## 8. Non-functional requirements

| Area | Requirement |
| --- | --- |
| Architecture | Clean architecture: `Domain` ← `Application` ← `Infrastructure` / `Api`. Domain has zero package references. |
| Testing | Every business rule unit-tested; every endpoint integration-tested against real PostgreSQL (Testcontainers), including 401/403/404/409 cases. Domain tests run in under 5 seconds. |
| Code quality | Nullable enabled, warnings as errors, analyzers on, formatter clean. Follows the clean code checklist in `plan.md`. |
| Errors | API errors use RFC 9457 ProblemDetails. Domain rule violations map to `409 Conflict` or `422`; validation to `400`. |
| Security | ASP.NET Core Identity, cookie auth, role-based authorization on every endpoint. No secrets in the repo. |
| Observability | Structured logs with a correlation ID per request. `/health` endpoint. |
| Performance | NCR list responds in < 500 ms for 10,000 NCRs (paged, indexed). |
| Accessibility | Forms labelled, keyboard usable, colour not the only status signal. |
| Time | All timestamps stored as UTC `DateTimeOffset`; displayed in Asia/Kuala_Lumpur. The domain never reads the system clock directly. |

## 9. Tech stack (fixed)

- Backend: ASP.NET Core Web API, .NET 10, C#
- Data: EF Core + PostgreSQL (Docker for local and tests)
- Frontend: React + TypeScript (Vite), TanStack Query, React Hook Form + Zod
- Tests: xUnit, Testcontainers, `Microsoft.AspNetCore.Mvc.Testing`
- CI/CD: GitHub Actions; deploy to Azure Container Apps

Do not add other frameworks or libraries (MediatR, AutoMapper, FluentAssertions, Redux, UI kits, etc.) without an ADR approved by the owner.

## 10. Success criteria

- All stories in section 7 pass their acceptance criteria in the deployed app
- CI green on `main`; zero build warnings
- README with live link, demo video, diagrams and 4–6 ADRs
- Owner can explain every design decision in an interview

## 11. Open questions

- Photo storage in v1: local disk volume or Azure Blob Storage? (default: local volume behind an `IFileStore` interface)
- Seed users for the public demo: shared demo credentials per role? (default: yes, read-only reset nightly — post-v1)
