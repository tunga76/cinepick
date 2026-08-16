# ADR 0004: Identity cookie authentication

- Status: Accepted
- Date: 2026-08-15

## Decision

CinePick uses ASP.NET Core Identity persisted in the application SQL Server database.
The browser session is an HttpOnly, SameSite=Strict cookie. The Angular application
does not store access or refresh tokens. JSON mutation endpoints validate an antiforgery
token sent through the `X-CSRF-TOKEN` header. Administrative authorization uses the
`Admin` role and policy; client-side guards are only a usability layer.

## Consequences

- The same-origin frontend/API deployment remains the default topology.
- TLS is mandatory in production. Local HTTP uses `SameAsRequest` cookie security so
  Docker development remains usable.
- The client fetches `/api/auth/csrf` before registration, login, logout, or another
  protected mutation. It refreshes the token after authentication state changes.
- Authentication failures return 401/403 instead of HTML redirects.
