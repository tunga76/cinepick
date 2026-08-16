# Release Checklist

## Before deployment

- Run backend build/tests, Angular build/unit tests, and the Docker-backed Playwright suite.
- Confirm NuGet, pnpm, Gitleaks, and both Trivy image scans pass.
- Keep `Database__Initialize=false`; apply reviewed EF migrations as an explicit deployment step.
- Back up SQL Server and verify that the restore procedure matches the target environment.
- Store SQL, AI, provider, telemetry, and bootstrap credentials in the platform secret store.
- Keep `Identity__BootstrapAdmin__Enabled=false` after the initial administrator is provisioned.
- Configure TLS/HSTS at the ingress and retain the application CSP and security headers.
- Verify ticket hosts, CORS/origin topology, data retention, and provider attribution for the target environment.

## Smoke checks

```text
GET /health/live                         -> 200
GET /health/ready                        -> 200
GET /api/movies/now-playing?pageSize=1   -> at least one item
POST /api/recommendations                -> at most three allowlisted results
```

Validate `/openapi/v1.json` in the Development/CI contract environment before deployment;
the endpoint is intentionally not mapped in Production.

Also verify login/logout, an authenticated preference update, an Admin-policy sync, mobile
layout, keyboard skip navigation, and a denied-location fallback. Never use production
credentials in automated smoke-test output.

## Rollback

- Roll back the application images to the previously approved immutable tags.
- Prefer a forward-fix migration. Execute a database down migration only after checking
  data-loss behavior and restoring from backup if required.
- Disable a failing external adapter by returning its mode to `Mock`; startup must remain
  independent of optional provider keys.
- Record the incident window, image identifiers, migration state, and fallback rate.

## Current MVP limitations

- Movie, showtime, and ticket data are fictional/mock unless a licensed adapter is added.
- Route-duration maps, Redis/distributed cache, and group recommendations are post-MVP.
- Production SLO thresholds, alert routing, retention periods, and the final KVKK text
  require deployment-owner approval.
