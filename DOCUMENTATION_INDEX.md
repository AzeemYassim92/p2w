# P2W Cards Documentation Index

Current date: 2026-06-15.

Use this as the top-level map for the repo docs.

## Primary Docs

| Document | Purpose |
| --- | --- |
| `README.md` | Project overview, local run instructions, major API examples, current limitations, and roadmap. |
| `SYSTEM_DESIGN.md` | Current architecture, bounded contexts, data flow, service boundaries, frontend route map, and near-term technical direction. |
| `MARKETPLACE_AGGREGATION_CONTEXT.md` | Deep marketplace aggregation context: tables, fields, provider model, API surfaces, and design notes for market intelligence. |
| `PRODUCT_DATA_COMPLETENESS_PLAN.md` | Checklist and strategy for improving product detail completeness before layering on more features. |
| `PRODUCT_TODO.md` | Living product backlog for buttons, unfinished workflows, commerce, account, market, catalog, and ops tasks. |
| `APPLICATION_WIREFRAMES.md` | Text wireframes for the current app surfaces and where the major UX pieces live. |
| `HANDOFF_NEXT_SESSION.md` | Paste-friendly handoff for a future Codex session if this chat/task context is lost. |
| `CHANGELOG.md` | Human-readable change history for what was built, where, and when. |
| `CONFIGURATION_AND_SECRETS.md` | Local configuration, provider keys, safe GitHub push practices, and secrets handling. |

## Recommended Reading Order

1. `README.md`
2. `HANDOFF_NEXT_SESSION.md`
3. `SYSTEM_DESIGN.md`
4. `APPLICATION_WIREFRAMES.md`
5. `PRODUCT_DATA_COMPLETENESS_PLAN.md`
6. `MARKETPLACE_AGGREGATION_CONTEXT.md`
7. `PRODUCT_TODO.md`
8. `CHANGELOG.md`
9. `CONFIGURATION_AND_SECRETS.md`

## Current Documentation Gaps

- A real ERD diagram is still needed once the market tables settle.
- A provider-by-provider API contract doc is still needed for eBay, JustTCG, PokemonTCG, Scryfall, and future sources.
- A user-role/security design doc is still needed before auth, seller onboarding, checkout, or public hosting.
- A deployment/runbook doc is still needed before hosting outside local development.
