# CinePick

CinePick is a modular-monolith .NET 10 and Angular 22 application for finding suitable movies and real showtimes. The repository includes the catalog, cinema/showtime, recommendation, Identity-based user and administration slices, plus the first Milestone 6 PWA and security hardening work. External API keys remain optional because mock providers are the default.

## Requirements

- .NET SDK 10.0.101 or a compatible 10.0 patch
- Node.js `^22.22.3`, `^24.15.0`, or `^26.0.0`
- pnpm 11+
- Docker with Compose

## Backend

```powershell
dotnet restore CinePick.sln --configfile NuGet.Config
dotnet build CinePick.sln --no-restore
dotnet test CinePick.sln --no-build --no-restore
dotnet run --project src/backend/CinePick.Api
```

Development endpoints:

- `GET /health/live`
- `GET /health/ready`
- `GET /openapi/v1.json`
- `GET /api/system/info`
- `GET /api/movies/now-playing`
- `GET /api/movies/upcoming`
- `GET /api/movies/{id}`

`/health/live` checks only the process. `/health/ready` also checks the configured SQL Server connection and is expected to report unhealthy when SQL Server is not running.

## Frontend

```powershell
pnpm --dir src/frontend/cinepick-web install --frozen-lockfile
pnpm --dir src/frontend/cinepick-web build
pnpm --dir src/frontend/cinepick-web test
pnpm --dir src/frontend/cinepick-web start
```

With the Docker environment running, install Playwright's Chromium once and run the
desktop and mobile critical-path tests:

```powershell
pnpm --dir src/frontend/cinepick-web exec playwright install chromium
pnpm --dir src/frontend/cinepick-web e2e
```

The E2E suite covers recommendations, authenticated-route redirection, location-denial
fallback, horizontal-overflow checks, and axe-core scans for serious/critical accessibility
violations. Set `CINEPICK_E2E_ADMIN_EMAIL` and `CINEPICK_E2E_ADMIN_PASSWORD` together
with the corresponding bootstrap variables to include the administrator sync scenario.
Service workers are disabled only in this suite; the production PWA assets are verified
separately by the Docker smoke checks. CI creates an isolated mock administrator, starts
the complete Compose topology, runs both desktop and mobile projects, and removes the
containers and volume afterward.

## Docker

Copy `.env.example` to `.env` and replace the local SQL Server password before sharing the environment.

```powershell
docker compose up --build
```

The frontend is available at `http://localhost:4200` and proxies `/api` and `/health` to the API. The API is also exposed at `http://localhost:8080`; its development OpenAPI document is available at `http://localhost:8080/openapi/v1.json`.

## Providers and authentication

Movie and cinema detail pages support Istanbul-local day and time-period filters,
language, format, maximum price, and time/price sorting. Movie details also support
cinema selection. “Seans filtrelerini temizle” resets these controls and sorting
without changing the selected day; result counts are announced to screen readers.
Showtime sorting compares actual instants across UTC offsets, using the showtime
identifier as a deterministic tie-breaker for equal start times (and equal prices
when sorting by price).
The shared mobile navigation closes with Escape and returns focus to its toggle.
Failed logout requests display an accessible retry message without clearing local
session state or redirecting the user before the server confirms logout.
Login and registration prevent duplicate submissions and mode changes while a
request is pending, and allow retry after a failed request.

Movie metadata, showtime, and AI modes default to `Mock`. Empty TMDb or AI keys do not prevent startup. To enable TMDb movie metadata, set `MovieProviders__Mode=TMDb` and store the application read access token in `TMDb__ReadAccessToken`; language, region, and page limits default to `tr-TR`, `TR`, and `2`. This affects movie metadata only—showtimes remain on their separately configured provider. To enable the OpenAI Responses ranker, set `AI__Mode=OpenAI` and `AI__ApiKey`; `AI__Model` and `AI__Endpoint` are optional.

`Database__Initialize=true` is set only in the local Compose environment so migrations and deterministic mock seed data are applied automatically. Production keeps this disabled; migrations should be applied as an explicit deployment step.

Authentication uses ASP.NET Core Identity with an HttpOnly same-origin session cookie and CSRF-protected JSON mutations. The API provides `/api/auth/csrf`, `/register`, `/login`, `/logout`, and `/me`; see `docs/SECURITY.md`. Legacy development operations remain development-only, while `/api/admin/*` is protected by the server-side `Admin` policy.

The Angular client exposes `/account` for registration/login and a guarded `/profile`
route. It obtains a fresh antiforgery token for every authentication mutation and never
stores session tokens in browser storage.

Authenticated users can store normalized recommendation preferences and manage a
per-movie favorite, watched, and 1–10 rating state. Movie detail and profile screens use
the same CSRF-protected API; user identifiers are always derived from the server session.
Favorite and rating signals can influence candidate ranking without bypassing mandatory
filters. Authenticated recommendation sessions appear in the user's private profile history.

Administration is exposed at `/admin` and `/api/admin/*` with the server-side `Admin`
policy and CSRF protection. No default administrator credentials exist. Optional one-time
bootstrap uses the `Identity__BootstrapAdmin__*` environment variables documented in
`.env.example`; keep them in a secret store and disable bootstrap after provisioning.

Production Angular builds include a PWA manifest and service worker. Only the app shell
and versioned static assets are cached; `/api`, recommendation, and time-sensitive
showtime responses are deliberately network-only. API/Nginx security headers and
separate auth, recommendation, and admin rate limits are enabled by default.

Keyboard users receive a visible global focus treatment and an “Ana içeriğe geç” link on
the catalog and cinema list routes. Reduced-motion preferences disable decorative motion.
The primary showtime time-window query is backed by an `(IsCancelled, StartsAt)` index;
catalog and recommendation reads use projection and no-tracking queries.

CI runs NuGet and pnpm vulnerability checks plus a full-history Gitleaks scan. Local
equivalents for dependency auditing are:

```powershell
dotnet list CinePick.sln package --vulnerable --include-transitive
pnpm --dir src/frontend/cinepick-web audit --audit-level high
```

The CI E2E job also scans the built API and frontend images for high/critical findings and
retains the Playwright HTML report for 14 days. SQL integration tests assert that the full
migration chain and the primary showtime-window index exist on a clean SQL Server.

Structured logs are written to stdout through Serilog. OpenTelemetry traces and metrics are collected in-process; set `OpenTelemetry__Otlp__Enabled=true` and standard `OTEL_EXPORTER_OTLP_*` variables to export them.

See [the implementation plan](docs/PLAN.md) for scope and milestone acceptance criteria,
and [the release checklist](docs/RELEASE-CHECKLIST.md) for deployment gates, smoke checks,
rollback, and known MVP limitations.
