# 6. Cookie sessions over JWT

- **Status:** Accepted
- **Date:** 2026-10-29

## Context

QualiTrack's only client is its own React app, served from the same origin as the API (the Vite
proxy in development, one deployment on Azure Container Apps in production). Users log in with an
email and password (PRD E1); every request must say who they are and which of the three roles they
have, and the API must answer `401` when they are signed out and `403` when their role is wrong.

The web app has to keep some proof of login between requests. The two usual choices differ in
where that proof lives, what an attacker can do with it, and whether the server can take it back:

- **A session cookie** set by the server. The browser stores it and attaches it to every request
  to the API; page scripts never touch it.
- **A JWT (JSON Web Token)**, a signed token the API returns in the login response. The app stores
  it itself (in memory, `localStorage` or `sessionStorage`) and sends it in an `Authorization:
  Bearer` header.

The questions that decide it:

| | Session cookie (`HttpOnly`) | JWT in browser storage |
| --- | --- | --- |
| **Where it lives** | Browser cookie jar, out of reach of page scripts | JavaScript-readable storage, or memory (lost on reload, so usually paired with a refresh token, often itself a cookie) |
| **XSS** (injected script runs in the page) | Script can send requests as the user while the page is open, but cannot read or steal the cookie | Script can read the token and send it anywhere; the attacker keeps using it from their own machine until it expires |
| **CSRF** (another site makes the browser send a request) | Browser attaches the cookie automatically, so it must be defended: `SameSite=Strict`, plus antiforgery tokens on writes | Not exposed: other sites cannot make the browser add the header |
| **Revocation** | Logout removes the cookie; a stolen copy is refused once the user's security stamp changes (password change, admin action), checked every 30 minutes | A signed token is valid until it expires; taking it back needs a server-side deny-list, which brings back the server state JWT was meant to avoid |
| **Other clients** (mobile, third-party) | Awkward: cookies are a browser mechanism | Natural fit |

## Decision

Users sign in with **ASP.NET Core Identity's application cookie**: one encrypted, server-issued
session cookie, no tokens handed to JavaScript.

- **The cookie is hardened** (`AuthenticationSetup`): `HttpOnly` so scripts cannot read it,
  `Secure` (`Always`) so it never travels over plain HTTP, `SameSite=Strict` so the browser does
  not attach it to requests started by other sites. It is a session cookie (`isPersistent: false`),
  dropped when the browser closes.
- **No login redirects.** The React app owns the login page, so the API answers `401` and `403`
  with status codes and the app decides what to show.
- **Login** (`AuthEndpoints`) checks the password with lockout on (5 failures lock the account for
  5 minutes), refuses a user without exactly one role before any cookie is issued, gives the same
  `401` for an unknown email and a wrong password, and is rate-limited per client address
  (`LoginRateLimit`, 5 a minute).
- **The cookie carries the user id and role** as claims. `HttpCurrentUser` reads them for the
  handlers; `/api/auth/me` returns them so the app knows who is signed in without reading the cookie.
- **CSRF is defended in two layers.** `SameSite=Strict` is the first; antiforgery tokens on
  cookie-authenticated writes are the second (planned in the API section of `plan.md`), for older
  browsers and for same-site attacks from subdomains.

## Consequences

**Good**

- The most likely front-end bug, an XSS hole, cannot leak a credential: the attacker can act only
  while the victim's page is open, not replay a stolen token later.
- The server can end sessions: logout removes the cookie, and changing a user's security stamp
  invalidates every copy within 30 minutes.
- Nothing to build: Identity issues, encrypts, validates and renews the cookie, and the app code
  never stores or attaches credentials.

**Bad**

- CSRF must be handled, because the browser sends the cookie on its own. `SameSite=Strict` covers
  modern browsers; the antiforgery task is still required, and every new write endpoint must use it.
- The role in the cookie can be up to 30 minutes stale: a role change applies at the next sign-in
  or security-stamp check. Handlers that look up *another* user (A2) use `IUserDirectory`, which
  always reads the database.
- The app and API must share an origin (proxy in development, one domain in production). A separate
  API domain would need CORS with credentials and `SameSite=None`, giving up the first CSRF layer.
- The cookie is encrypted with ASP.NET Core Data Protection keys. In production the keys must be
  persisted and shared by every instance, or each restart or new replica signs everyone out. To be
  settled with the Azure deployment task.
- The ticket inside the cookie is good for 14 days, renewed while in use (Identity's defaults). A
  copy taken from a device stays usable for that long unless the security stamp changes. A shorter
  `ExpireTimeSpan` fits a shop-floor app better; to be decided with the owner.
- Only browsers are first-class clients. A mobile app or an integration would need a token-based
  scheme added beside the cookie.

## Alternatives considered

- **JWT in `localStorage`:** simplest to wire into a SPA and no CSRF exposure, but any XSS can steal
  a token that keeps working from anywhere until it expires, and logout cannot revoke it.
- **JWT in memory with a refresh token in an `HttpOnly` cookie:** removes the theft-by-XSS risk for
  the access token, but the refresh cookie brings CSRF back, and the app must handle refreshes and
  races between tabs. More moving parts for the same protection the session cookie gives.
- **JWT inside an `HttpOnly` cookie:** cookie behaviour (CSRF included) with a token format, but
  revocation still needs a deny-list; it gains nothing over Identity's own cookie for one first-party
  browser client.
- **Identity's bearer-token endpoints (`MapIdentityApi`):** ready-made, but by default they issue opaque bearer
  tokens to JavaScript (the XSS exposure above) and expose endpoints, such as registration, that v1
  does not want.
