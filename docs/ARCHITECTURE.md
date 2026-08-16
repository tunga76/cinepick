# CinePick Architecture

## Style

CinePick is a modular monolith. The backend is deployed as one ASP.NET Core process, while module boundaries are maintained in code and verified by architecture tests.

The dependency direction is:

```text
Api -> Infrastructure -> Application -> Domain
                  \------^             ^
```

- Domain owns provider-independent business concepts and rules.
- Application owns vertical-slice use cases, ports, DTOs, validation, and results.
- Infrastructure owns EF Core, SQL Server, jobs, caches, and external adapters.
- API owns HTTP contracts, composition, Problem Details, authorization policies, and observability setup.

## Runtime topology

Angular is served by nginx. `/api` and `/health` requests are proxied to ASP.NET Core. SQL Server is the system of record. Movie, showtime, and AI integrations remain replaceable providers and default to mock mode when credentials are absent.

## Health model

- `/health/live` verifies that the process can answer HTTP and deliberately runs no dependency checks.
- `/health/ready` verifies the SQL Server connection and will include future critical readiness dependencies.

## Observability

Serilog writes structured console events suitable for container collection. OpenTelemetry instruments incoming ASP.NET Core requests, outgoing HTTP calls, and runtime metrics. OTLP export is disabled by default and can be enabled with standard `OTEL_EXPORTER_OTLP_*` variables plus `OpenTelemetry__Otlp__Enabled=true`.
