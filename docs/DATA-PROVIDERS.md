# Data Providers

## Current mode

Movie metadata defaults to deterministic, fictional data identified by provider key `mock`.
Setting `MovieProviders__Mode=TMDb` together with `TMDb__ReadAccessToken` selects the
TMDb adapter. The seed dataset remains available, and showtime data stays on the separate
mock provider until a licensed showtime integration is configured.

## Provider boundary

`IMovieMetadataProvider` isolates provider DTOs from the domain. External provider
identifiers remain separate from local `Movie.Id`; `(ExternalProviderId, ExternalMovieId)`
is protected by a unique database index. Missing credentials select the mock provider
and do not prevent startup. The TMDb adapter reads Turkish now-playing and upcoming
lists, then enriches each entry with details and Turkish theatrical certification data.

## Movie metadata synchronization

Synchronization maps provider records into Application import models and performs an
idempotent upsert using the external provider key. Each run records only operational
metadata—status, timestamps, received/inserted/updated counts, and a stable error code—in
`ExternalSyncLogs`; complete provider payloads are not stored.

For TMDb, only known local genre identifiers are imported. Records without a usable
title, release date, or positive runtime are skipped. Relative poster/backdrop paths are
stored, while arbitrary absolute image URLs are rejected. Movies no longer returned by
the same provider have their now-playing and upcoming flags cleared.

Legacy manual endpoints are available only in the ASP.NET Core Development environment.
Production-capable synchronization is exposed under `/api/admin/*` and requires the
server-side `Admin` policy plus a valid antiforgery token for mutations.

## Licensing

Before enabling TMDb in a user-facing deployment, include the approved TMDb attribution
and logo in the UI and verify the current image, caching, and content usage requirements.
The required notice is: “This product uses the TMDB API but is not endorsed or certified
by TMDB.” TMDb is a metadata provider only; no showtime scraping is permitted.

## Showtime synchronization

`IShowtimeProvider` returns provider-neutral showtime import records. Synchronization
uses a filtered unique external sync key, maps movie and auditorium references against
server-owned records, validates ticket links against the configured HTTPS host allowlist,
and records operational counts in `ExternalSyncLogs`. The mock adapter requires no key.
