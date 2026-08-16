# CinePick API

## Conventions

- Base path: `/api`
- JSON uses camelCase property names.
- List endpoints support `page` and `pageSize`; defaults are 1 and 12, maximum page size is 50.
- Validation failures use RFC Problem Details with field errors and `errorCode=validation.failed`.
- Missing resources use RFC Problem Details with a stable module error code.

## Movies

### `GET /api/movies/now-playing`

Returns now-playing movies ordered by popularity and title.

Query parameters:

- `page`: positive integer
- `pageSize`: 1–50
- `search`: optional title search
- `genreId`: optional genre identifier
- `maximumRuntimeMinutes`: optional positive runtime limit

### `GET /api/movies/upcoming`

Uses the same query contract and returns upcoming movies.

### `GET /api/movies/{id}`

Returns one movie detail response or `404 movies.not_found`.

## Genres

### `GET /api/genres`

Returns all catalog genres ordered by name. The identifiers can be passed to movie
list endpoints through the `genreId` query parameter.

## Authentication

- `GET /api/auth/csrf`: creates the antiforgery cookie and returns the corresponding
  request token. Send it as `X-CSRF-TOKEN` on authentication mutations.
- `POST /api/auth/register`: accepts `email`, `password`, and `displayName`, creates an
  Identity user, and starts an HttpOnly cookie session.
- `POST /api/auth/login`: accepts `email` and `password` and starts a cookie session.
- `POST /api/auth/logout`: requires authentication and ends the session.
- `GET /api/auth/me`: requires authentication and returns the current user's id, email,
  display name, and roles.

Registration, login, and logout require a valid antiforgery token. Authentication and
authorization failures return 401 and 403 without redirects.

## User profile

All routes require the Identity session and derive the user id from its server-validated
claim. No request accepts a user id.

- `GET /api/users/me/preferences`
- `PUT /api/users/me/preferences`: updates preferred genre/language, maximum runtime,
  and maximum distance.
- `GET /api/users/me/movie-states`: returns the current user's favorite, watched, and
  rated movies.
- `GET /api/users/me/movie-states/{movieId}`
- `PUT /api/users/me/movie-states/{movieId}`: idempotently replaces `isFavorite`,
  `isWatched`, and optional 1–10 `rating` values.

Both `PUT` routes require `X-CSRF-TOKEN`.

- `GET /api/users/me/recommendation-history`: returns the latest 20 recommendation
  sessions belonging to the current user and their ranked results.

## Development-only catalog synchronization

### `POST /api/development/movie-catalog-syncs`

Triggers the configured mock metadata provider and returns insert/update counts. This
legacy development route is mapped only in the ASP.NET Core Development environment.
Production-capable operations use the policy-protected `/api/admin/*` routes below.

### `POST /api/development/showtime-catalog-syncs`

Runs the configured mock showtime provider with the same Development-only restriction.
Ticket links must use HTTPS and match `ShowtimeProviders:AllowedTicketHosts`.

- `GET /api/development/sync-logs`: returns the latest 20 operational sync summaries.
- `GET /api/development/showtimes`: returns showtimes including cancellation state.
- `PUT /api/development/showtimes/{id}/cancellation`: cancels or restores a showtime.

All `/api/development/*` routes are absent in Production rather than relying on a client-side guard.

## Administration

Production-capable administration routes use `/api/admin/*` and require the server-side
`Admin` policy. They provide catalog/showtime synchronization, recent sync logs, showtime
listing, and cancellation updates. Every admin mutation also requires `X-CSRF-TOKEN`.
The Angular `/admin` guard is only a usability check; API authorization is authoritative.

## Recommendations

### `POST /api/recommendations`

Accepts `{ "text": "..." }` with a maximum of 500 characters. The mock parser recognizes
relative day/time, maximum runtime and price, genre, city/district, language, and IMAX
constraints. Mandatory filters are applied by SQL before at most 20 candidates are scored.
The result contains at most three verified candidates. Mock mode uses `mock-ai`; provider
failure or invalid output uses the deterministic fallback scorer.

## Health

- `GET /health/live`: process liveness only
- `GET /health/ready`: SQL Server readiness

## Cinemas and showtimes

- `GET /api/cities`: lists selectable cities.
- `GET /api/cinemas?cityId={id}`: lists cinemas, optionally scoped to a city. Supplying
  `latitude`, `longitude`, and an optional `radiusKilometers` (maximum 100) returns
  nearby cinemas ordered by Haversine distance.
- `GET /api/cinemas/{id}`: returns cinema details and auditoriums.
- `GET /api/showtimes`: lists non-cancelled showtimes. Optional `cinemaId`, `movieId`,
  `from`, and `to` filters are supported; the requested interval is limited to eight days.
