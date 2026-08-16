# Security

## Authentication and authorization

ASP.NET Core Identity stores users, password hashes, roles, claims, tokens, logins, and
lockout state in SQL Server. Passwords require at least ten characters plus uppercase,
lowercase, digit, and non-alphanumeric characters. Five failed attempts lock the account
for fifteen minutes.

The `CinePick.Session` cookie is HttpOnly, SameSite=Strict, non-persistent, and expires
after eight hours with sliding expiration. Production deployments must terminate HTTPS.
API authentication and access-denied responses are 401 and 403; cookie middleware never
redirects API callers to an HTML login page.

All cookie-authenticated JSON mutations must validate the double-submit antiforgery
flow: first obtain `/api/auth/csrf`, then send its token as `X-CSRF-TOKEN`. Tokens must be
refreshed after login, registration, and logout because the authenticated identity changed.

`Admin` authorization is enforced by the API policy and cannot rely on an Angular route
guard. Development-only catalog mutation endpoints remain unmapped outside Development
and the production administration equivalents live under `/api/admin/*`.

The `Admin` role is initialized idempotently with local database initialization. An
initial administrator account is never created by default. To bootstrap one, explicitly
set `Identity__BootstrapAdmin__Enabled=true` and provide email/password through secrets or
environment variables. Bootstrap is idempotent and must be disabled after provisioning.

## Sensitive data

Never log passwords, session or antiforgery cookies, tokens, complete recommendation
prompts, or exact coordinates. Secrets are supplied through user secrets or environment
variables and never committed.

## Browser and abuse controls

Authentication, recommendation, and administration routes use separate fixed-window
rate-limit policies partitioned by authenticated user id or forwarded client IP. The API
returns 429 when a partition is exhausted. Forwarded headers are processed before
authentication and limiting in the Nginx deployment topology.

API and frontend responses set `nosniff`, frame denial, referrer, and permissions headers.
Nginx also applies a restrictive Content Security Policy. Production TLS and HSTS remain
deployment-edge responsibilities.

Every `/api` response sends `Cache-Control: no-store` and `Pragma: no-cache`. This keeps
authenticated profile/admin data and time-sensitive catalog, showtime, and recommendation
responses out of browser and intermediary caches; only versioned PWA shell assets are cached.

## CI security gates

CI fails on known high/critical pnpm vulnerabilities and reports vulnerable direct or
transitive NuGet packages. Gitleaks v3 scans full Git history for committed credentials.
The E2E job builds both production images and scans them with a full-commit-pinned Trivy
action; fixable high or critical findings fail the job. These checks complement dependency
update review and do not replace provider key rotation or production registry policies.
