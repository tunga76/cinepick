# Data Providers

## Current mode

Milestone 2 uses deterministic, fictional data identified by provider key `mock`. The
seed dataset contains 20 movies and eight genres; the mock synchronization feed adds
one more fictional movie. Generated visual placeholders are used instead of copyrighted posters.

## Provider boundary

`IMovieMetadataProvider` isolates provider DTOs from the domain. External provider
identifiers remain separate from local `Movie.Id`; `(ExternalProviderId, ExternalMovieId)`
is protected by a unique database index. Missing credentials select the mock provider
and do not prevent startup. A future TMDb adapter must implement the same port.

## Movie metadata synchronization

Synchronization maps provider records into Application import models and performs an
idempotent upsert using the external provider key. Each run records only operational
metadata—status, timestamps, received/inserted/updated counts, and a stable error code—in
`ExternalSyncLogs`; complete provider payloads are not stored.

Legacy manual endpoints are available only in the ASP.NET Core Development environment.
Production-capable synchronization is exposed under `/api/admin/*` and requires the
server-side `Admin` policy plus a valid antiforgery token for mutations.

## Licensing

Before enabling TMDb, its current attribution, image URL, caching, and content usage
requirements must be verified against official provider terms and represented in the UI.
No showtime scraping is permitted.

## Showtime synchronization

`IShowtimeProvider` returns provider-neutral showtime import records. Synchronization
uses a filtered unique external sync key, maps movie and auditorium references against
server-owned records, validates ticket links against the configured HTTPS host allowlist,
and records operational counts in `ExternalSyncLogs`. The mock adapter requires no key.
