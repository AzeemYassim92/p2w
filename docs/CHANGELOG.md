# Changelog

This changelog records product and engineering checkpoints for P2W Cards.

Dates use local project time.

## 2026-06-15

### Marketplace Intelligence

- Added catalog-level market aggregation surfaces for product pricing, market rankings, set dashboards, deal scanning, provider health, and catalog watchlist intelligence.
- Added/expanded market aggregation APIs for refresh, summary, confidence, comparison, chart, rankings, deals, set dashboards, provider health, and runs.
- Added market entities, EF mappings, migration, and aggregation worker project.
- Added market diagnostics trail for refresh debugging.
- Added eBay active listing integration path using official API access.
- Added JustTCG reference provider configuration path.
- Added PokemonTCG reference price provider behavior and improved handling of missing/empty provider price payloads.
- Added refresh throttling/skipping logic so set refreshes do not repeatedly scan fresh rows unless forced.
- Added continuation behavior so one failed product does not stop an entire set refresh.
- Added temporary local session logging to `logs/session.log`, recreated on API launch.
- Added frontend/API connection logging, API request timing logs, provider call breadcrumbs, import breadcrumbs, and eBay listing normalization counts.
- Added catalog maintenance endpoints for Pokemon completeness reporting and dry-run metadata backfill.
- Capped market confidence when no sold comps are present so active listings alone cannot produce a high-confidence signal.
- Reduced deal/liquidity scoring inflation from active listings without sold comps.

### Frontend

- Reworked the marketplace UI into a black/gold shell with white product cards.
- Added global nav with marketplace, browse, sell, market, deals, sets, import, mappings, providers, and alerts.
- Added search bar, country/currency selector, wishlist button, cart scaffold, login scaffold, and signup scaffold.
- Added a P2W Collectibles marketplace banner.
- Improved product cards with larger images, clearer spacing, blue P2W detail button, green provider price box, and card art hover zoom.
- Added product pricing page separate from the general product detail page.
- Added market rankings page and moved set card listings below the analytics sections.
- Added provider health, deal scanner, set dashboard, and watchlist intelligence pages.
- Added app error boundary to catch render failures and show a recoverable UI error.

### Catalog and Product Data

- Imported Pokemon catalog data through PokemonTCG checkpoints.
- Added terminal catalog sync and validation commands for Pokemon and One Piece.
- Added an official One Piece catalog import provider that reads Bandai card list set pages and canonicalizes alternate-art rows into one marketplace product per set/card/name.
- Added catalog validation output comparing provider/official set and card counts against local database counts.
- Changed catalog validation to compare provider cards against local single-card products, excluding sealed marketplace products from card totals.
- Updated catalog import set upserts so existing seeded sets can be refreshed with provider codes, release dates, and images.
- Validated `Phantasmal Flames` set count and sample product data for Dawn `#118`.
- Added `PRODUCT_DATA_COMPLETENESS_PLAN.md`.
- Added `CATALOG_SYNC_RUNBOOK.md`.
- Added future-import support for Pokemon rules text into `CatalogProduct.Description`.
- Changed future Pokemon variant generation to prefer TCGplayer price keys before generic inferred variants.
- Identified the need to backfill existing Pokemon rows for richer metadata and corrected variants.
- Added a Pokemon metadata backfill path that enriches existing mapped rows from PokemonTCG and adds missing variants without deleting existing data.

### Documentation

- Added `DOCUMENTATION_INDEX.md`.
- Added `SYSTEM_DESIGN.md`.
- Added `APPLICATION_WIREFRAMES.md`.
- Added `HANDOFF_NEXT_SESSION.md`.
- Added `CHANGELOG.md`.
- Added `CONFIGURATION_AND_SECRETS.md`.
- Updated `PRODUCT_TODO.md` with the product data completeness gate.

### Configuration and Safety

- Added local appsettings override loading to API startup.
- Preserved local provider credentials in ignored `src/P2W.Cards.Api/appsettings.Local.json`.
- Sanitized tracked `src/P2W.Cards.Api/appsettings.json` so provider credentials are not committed.
- Added `src/P2W.Cards.Api/appsettings.Local.example.json`.

### Validation

- `dotnet build --no-restore -c Release` passed.
- `dotnet test --no-build --no-restore -c Release` passed with 47 tests.
- `npm run build` in `client` passed.

## 2026-06-14

### Marketplace Aggregation Foundation

- Added marketplace aggregation context documentation.
- Added catalog market data model concepts for listings, sales, snapshots, metrics, provider runs, errors, and checkpoints.
- Added provider scaffolds for eBay, PriceCharting, PokemonTCG reference pricing, and mock market data.
- Added catalog watchlist intelligence flow.
- Added market ranking and provider health scaffolds.

### Catalog Import

- Continued PokemonTCG catalog imports with checkpoints.
- Investigated no-data provider refresh results and identified source coverage gaps.
- Confirmed that catalog identity matching and market evidence collection need to be separate pipeline stages.

### Product Planning

- Added `PRODUCT_TODO.md` as the living backlog for UI buttons, marketplace aggregation, commerce, accounts, catalog quality, and testing.
- Captured provider and marketplace aggregation design notes in `MARKETPLACE_AGGREGATION_CONTEXT.md`.
