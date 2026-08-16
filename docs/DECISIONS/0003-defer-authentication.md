# ADR 0003: Defer authentication to Milestone 5

- Status: Superseded by ADR 0004
- Date: 2026-08-14

## Decision

Milestones 1–4 operate without authentication. No temporary trust headers or fake authorization mechanisms will be introduced. Identity and policy-based authorization will be designed and implemented together in Milestone 5.

## Consequences

Catalog and recommendation work is not blocked by an unsettled browser-session model. Administrative mutation endpoints must not be represented as production-ready before authorization exists.
