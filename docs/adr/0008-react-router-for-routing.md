# 8. React Router for routing

- **Status:** Accepted
- **Date:** 2026-10-30

## Context

The web app is growing from one page to several: login, raise an NCR, NCR detail, then the list,
the dashboard and the investigator and verification screens (PRD §6). Each needs its own address,
so it can be bookmarked, shared, refreshed and reached with the browser's back button, and some are
for one role only (PRD §4). PRD §9 fixes the frontend stack (React, TypeScript, Vite, TanStack
Query, React Hook Form, Zod) but names no router, and says any other library needs an ADR the owner
approves.

The options the owner weighed:

| | React Router | TanStack Router | Hand-written router (History API) |
| --- | --- | --- | --- |
| **Adoption** | The most used React router; what most tutorials, teams and interviewers expect | Growing, smaller community | None: our own code |
| **Type safety** | Route params are strings, so pages check them | Routes, params and search params are typed end to end | Whatever we build |
| **Protected routes** | A layout route that renders `<Outlet />` or redirects | `beforeLoad` guards | Our own code |
| **Setup** | Declare `<Routes>` in JSX | A route tree, often with a code generator | None, but links, params, 404s and focus are ours to get right |
| **Learning value** | Transfers to most React jobs | Pairs with TanStack Query, already in the stack | Teaches how client-side routing works, then wears thin as pages grow |

## Decision

Use **React Router** (the `react-router` package, v8) in declarative mode.

- **One route table** in `web/src/routes/AppRoutes.tsx`, wrapped in `<BrowserRouter>` and the
  `AuthProvider` in `App.tsx`. `AppLayout` is a layout route, so every page shares the landmarks
  and the nav.
- **Who is signed in** comes from `/api/auth/me`, asked once on load by `AuthProvider`, because the
  session cookie is `HttpOnly` and the app cannot read it (ADR 0006). The answer is one of
  `checking`, `signedOut`, `signedIn` (with the user and role) or `unavailable`. A server error is
  `unavailable`, never `signedOut`, so a broken API does not look like a logged-out user.
- **Protected routes** are layout routes using `ProtectedRoute`, with an optional `allow` list of
  roles. A signed-out user is redirected to `/login`, and the address they wanted is kept in the
  navigation state so login can send them back. A signed-in user with the wrong role sees a
  "You cannot open this page" message at the same address rather than a redirect.
- **The nav shows only what the role can use** (the raise link is for Inspectors), matching the
  slice rule that controls are hidden from roles that cannot use them.
- **The API stays the authority.** Route guards are for the user's benefit; every endpoint still
  checks the session and the role, so a page reached by mistake cannot read or change anything.
- **Declarative mode, not data or framework mode.** Data loading belongs to TanStack Query (PRD
  §9), so React Router's loaders and actions are not used, and there is no server rendering.

## Consequences

**Good**

- Every page has a real URL that survives refresh and works with back and forward.
- Role rules for pages sit in one table next to the routes they protect, where a reviewer can
  check them against PRD §4.
- The most widely known router: easy for others to read, and the skills transfer.
- Guards are covered by tests that render the real route table with a stubbed API
  (`AppRoutes.test.tsx`).

**Bad**

- One more runtime dependency, about 40 kB of the JavaScript bundle (before gzip).
- Route params are untyped strings; pages must validate them (an NCR id that is not a GUID) before
  calling the API.
- The role in the app is only as fresh as the last `/api/auth/me` call, and the role in the cookie
  can be up to 30 minutes stale (ADR 0006). A changed role may show the old nav until the next load;
  the API still refuses what the new role may not do.
- When the app is served by the API (Phase 6), the API must return `index.html` for unknown
  non-`/api` paths, or refreshing a deep link gives a 404.
- On a route change, React Router does not move focus or announce the new page to screen readers.
  This is left to the Phase 5 keyboard and accessibility pass.

## Alternatives considered

- **TanStack Router:** typed routes and params, and the same family as TanStack Query, but less
  widely known, with more setup (route tree, code generation) than this app's handful of pages
  needs.
- **A hand-written router on the History API:** no dependency and a good lesson in how routing
  works, but links, params, nested layouts, 404s and guards would all be ours to write and test,
  and the cost grows with every page in slices S2–S7.
- **React Router's data mode (`createBrowserRouter` with loaders):** fetches data before a route
  renders, but that overlaps with TanStack Query, which the PRD fixes for server state. Two caching
  layers for the same data is a source of bugs.
