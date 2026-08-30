# CinePick Repository Guide

## Structure

- `src/backend`: .NET 10 modular-monolith projects.
- `src/frontend/cinepick-web`: Angular 22 standalone application.
- `tests`: unit, integration, architecture, and end-to-end tests.
- `docs`: architecture, API, provider, AI, security, and ADR documentation.

## Commands

```powershell
dotnet restore --configfile NuGet.Config
dotnet build --no-restore
dotnet test --no-build
pnpm --dir src/frontend/cinepick-web install --frozen-lockfile
pnpm --dir src/frontend/cinepick-web test
pnpm --dir src/frontend/cinepick-web build
pnpm --dir src/frontend/cinepick-web e2e
docker compose up --build
```

## Coding rules

- Keep dependencies directed `Api -> Infrastructure/Application -> Domain`.
- Organize Application use cases as vertical slices by module and feature.
- Do not introduce a generic repository; use EF Core directly in query implementations.
- Domain must not depend on EF Core, HTTP clients, AI SDKs, or provider DTOs.
- API routes use lowercase plural kebab-case resources.
- Never expose entities directly; use request/response contracts.
- Expected failures use `Result<T>`; unexpected failures use the global exception handler.
- Store instants in UTC and render them in `Europe/Istanbul` at the UI boundary.

## Tests and completion

- Add unit tests for domain rules and deterministic algorithms.
- Add SQL Server Testcontainers integration tests for persistence and API behavior.
- Add WireMock.Net tests for external provider failure modes.
- A change is complete only when affected builds and tests pass and documentation/config examples stay current.

## GitHub and deployment authorization

- The user authorizes committing completed project updates and pushing them to GitHub after affected tests and builds pass, without asking for confirmation for each update.
- This authorization does not include Azure deployment. Ask for explicit approval before deploying to Azure.
- After each GitHub update, provide PowerShell Azure Container Apps update commands for affected images using the pushed commit SHA. Tell the user to wait for successful image publication before running them; do not execute deployment without approval.
- Do not include unrelated user changes or secrets in commits; tool permission checks still apply.

## External service and AI safety

- Never commit secrets or real `.env` values.
- Keep movie, showtime, and AI providers behind separate Application ports.
- AI may rank only server-created candidates; validate every returned identifier against that candidate set.
- Validate provider URLs against an HTTPS host allowlist.
- Do not log complete user prompts, exact coordinates, tokens, or sensitive provider payloads.
- Do not add scraping without explicit permission and a verified legal/provider basis.

## Authentication status

Authentication is intentionally absent through Milestone 4. Do not add ad-hoc user trust headers or fake authorization. Identity and policy-based authorization are introduced as a coherent feature in Milestone 5; until then, admin mutations must not be exposed as production-ready endpoints.
