# ADR 0002: Use replaceable, mock-first providers

- Status: Accepted
- Date: 2026-08-14

## Decision

Movie metadata, showtime data, and AI capabilities are separate Application ports. Each capability has a deterministic mock implementation, and missing credentials never prevent application startup.

## Consequences

Development and tests remain reproducible. Provider DTOs and identifiers cannot become domain identities. Real-provider licensing, attribution, resilience, and mapping remain adapter responsibilities.
