# ADR 0001: Use a modular monolith

- Status: Accepted
- Date: 2026-08-14

## Decision

Build one deployable ASP.NET Core backend with explicit modules and layered dependency rules. Do not split the initial product into microservices.

## Consequences

Deployment, local development, transactions, and observability remain simple. Module boundaries must be guarded through architecture tests and review because process isolation does not enforce them.
