# AI Recommendation Boundary

`IRecommendationRanker` receives only server-created candidate DTOs, never database
access or arbitrary URLs. Ranker output is accepted only when it contains one to three
unique `(movieId, showtimeId)` pairs from that exact candidate allowlist, scores between
0 and 100, and non-empty reasons.

A two-second timeout, malformed output, duplicate or hallucinated identifiers, and
provider failures switch to deterministic scoring. The default adapter is `mock-ai`;
a real provider is not required for startup or tests.

## OpenAI Responses adapter

Set `AI__Mode=OpenAI` and provide `AI__ApiKey` to enable the real adapter. Optional
settings are `AI__Model` (default `gpt-5-mini`) and `AI__Endpoint` (default
`https://api.openai.com/v1/responses`). Selecting OpenAI without a key deliberately
keeps the mock adapter active, so local startup and CI never depend on a secret.

The adapter uses the Responses API with strict JSON Schema structured output. It sends
only the normalized filter and the server-created candidate DTOs. The original user
message is not included in the provider request. Responses are parsed as untrusted data;
the application layer still enforces the candidate allowlist, score bounds, uniqueness,
result count, and fallback behavior.

## Audit persistence

Each request creates a `RecommendationSession` containing normalized filters, method,
timestamps, candidate `(movieId, showtimeId)` snapshots, and the ranked result records.
The original natural-language request is deliberately absent from the persistence model.

Authenticated sessions store the server-derived user identifier and are exposed only
through `/api/users/me/recommendation-history`. Anonymous sessions keep a null user id.
Favorite movies receive an 8-point personalization signal and an existing 1–10 user
rating contributes its numeric value. These signals are computed by the server and sent
to a configured ranker only as part of the bounded candidate DTO; they never weaken the
mandatory SQL filters or candidate allowlist.
