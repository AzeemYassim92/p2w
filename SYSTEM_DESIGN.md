# P2W Cards System Design

Current date: 2026-06-15.

## Product Goal

P2W Cards is evolving from a trading-card marketplace MVP into a market intelligence and deal-scouting tool for collectible inventory.

The near-term business goal is:

- Browse trustworthy catalog data by game, set, product, and variant.
- Refresh real market evidence from approved providers.
- Compare reference prices, active listings, and sold comps.
- Surface cards worth buying for inventory based on margin, ROI, liquidity, and confidence.

Commerce, checkout, auth, seller onboarding, and fulfillment are intentionally later stages.

## Current Stack

- Backend: ASP.NET Core Web API on .NET 9.
- Frontend: React, TypeScript, Vite.
- Database: SQL Server through EF Core.
- Tests: xUnit with EF Core InMemory coverage.
- Local API ports: `http://127.0.0.1:5087`, `https://localhost:7263`.
- Local frontend port: usually `http://127.0.0.1:5173` or `http://127.0.0.1:5174`.

## Solution Layout

```text
src/
  P2W.Cards.Api                  HTTP controllers, OpenAPI, app startup
  P2W.Cards.Application          DTOs, interfaces, options
  P2W.Cards.Domain               Entities and enums
  P2W.Cards.Infrastructure       EF Core, providers, services, matching, diagnostics
  P2W.Cards.Worker.Aggregation   Background market refresh worker
tests/
  P2W.Cards.Tests                Unit/integration tests
client/
  src/                           React application
```

## Bounded Contexts

### Legacy Card MVP

Older card/search/listing/watchlist flows still exist around:

- `Card`
- `CardVariant`
- `Listing`
- `PriceSnapshot`
- `PriceReferenceSnapshot`

These are useful for legacy screens and early feature proof, but should not be the primary foundation for marketplace aggregation.

### Canonical Catalog

The newer marketplace catalog centers on:

- `Game`
- `CardSet`
- `ProductCategory`
- `CatalogProduct`
- `ProductVariant`
- `ExternalProductMapping`

This is the preferred foundation for product detail pages, set dashboards, marketplace cards, deal scouting, and future seller inventory.

### Marketplace Aggregation

The aggregation slice attaches market evidence to catalog products:

- Reference prices.
- Active marketplace listings.
- Sold/completed comps where available.
- Computed snapshots and metrics.
- Provider diagnostics and ingestion runs.
- Watchlist intelligence and deal opportunities.

Market identity confidence and market-price confidence must be treated separately.

## High-Level Data Flow

```text
Provider API
  -> provider client
  -> provider DTO
  -> normalizer
  -> product matching
  -> SQL Server raw/normalized rows
  -> market metric computation
  -> API DTOs
  -> React dashboard/detail UI
```

## Catalog Import Flow

```text
Catalog Import Page
  -> POST /api/catalog/import/preview
  -> provider reads remote data
  -> normalizer builds ExternalCatalogProductDto
  -> matcher predicts create/update/skip
  -> optional POST /api/catalog/import/run
  -> upsert sets, products, variants, mappings
  -> save run/checkpoint/errors
```

Current catalog providers:

- PokemonTCG for Pokemon metadata and images.
- Scryfall for Magic metadata and images.
- Seed/demo data for other areas.

## Market Refresh Flow

```text
Product Pricing Page or Set Dashboard
  -> POST /api/market/aggregation/products/{id}/refresh
  -> POST /api/market/aggregation/sets/{setId}/refresh
  -> MarketAggregationService
  -> reference providers
  -> active listing providers
  -> sold comp providers
  -> snapshots + metrics + diagnostics
  -> market summary/comparison/chart/deals endpoints
```

Current market providers:

- `PokemonTCG`: reference price provider when a price payload exists.
- `JustTcg`: reference provider scaffold/configured for future expansion.
- `eBay`: active listing provider using official API access.
- `MockMarket`: deterministic demo rows for development.
- `PriceCharting`: scaffolded reference provider.

Current gap:

- Sold comps are not yet reliable/enabled, so market confidence should remain conservative even when active listings are available.

## Frontend Route Map

The frontend currently uses local React state routing in `client/src/App.tsx`.

| Nav / Route | Component | Purpose |
| --- | --- | --- |
| Marketplace | `MarketplaceHomePage` | Home/catalog browsing, banner, categories, product cards, latest/upcoming sets. |
| Browse | `SearchPage` | Legacy card search and browse path. |
| Sell | `SellerInventoryPage` | Seller inventory scaffold. |
| Market | `MarketRankingsPage` | Market rankings, set insights, charts, and set catalog rows. |
| Deals | `DealScannerPage` | Deal opportunity search and filters. |
| Sets | `SetMarketDashboardPage` | Set lookup and per-set market dashboard. |
| Import | `CatalogImportPage` | Admin catalog import controls. |
| Mappings | `MappingReviewPage` | Admin mapping review workflow. |
| Providers | `ProviderHealthPage` | Provider readiness and health. |
| Alerts | `AlertsPage` | Legacy alert flow. |
| Wishlist | `CatalogWatchlistIntelligencePage` | Catalog watchlist market intelligence. |
| Product detail | `CatalogProductDetailPage` | TCGplayer-like product page. |
| Product pricing | `CatalogProductPricingPage` | Aggregated pricing, comparison, charts, data quality, deal signals. |

## Main API Areas

- `api/catalog/*`: games, sets, categories, products, capabilities.
- `api/catalog/import/*`: preview/run/import history.
- `api/catalog/mappings/*`: mapping review.
- `api/market/aggregation/*`: refresh product/set/recent/watchlisted/trending and read runs.
- `api/market/products/{id}/*`: summary, confidence, comparison, chart, deals.
- `api/market/rankings/*`: trending, high volume, movers, opportunities, deals.
- `api/market/sets/*/dashboard`: set-level dashboard.
- `api/catalog-watchlist/*`: catalog watchlist and intelligence.
- `api/market/providers/health`: provider health.
- `api/seller-inventory`: seller inventory scaffold.

## Current Technical Decisions

- Keep marketplace aggregation in .NET for the MVP so it shares EF Core, DTOs, provider options, and tests with the app.
- Store raw provider JSON for audit/backfill while also normalizing key display and analytics fields.
- Prefer official APIs and partner-approved feeds. Do not scrape websites.
- Keep real provider credentials out of tracked files. Use local overrides, environment variables, or user secrets.
- Keep cards/products clickable into internal product/pricing pages, not external marketplaces by default.

## Known Risks

- Product metadata is incomplete for rich card details.
- Existing Pokemon variants may need backfill because earlier imports inferred generic variants.
- Active listings can contain poor matches, lots, graded cards, language mismatches, or outliers.
- Sold-comps are required before ROI/deal signals can be trusted at scale.
- Market confidence currently needs a more explicit split between identity match quality and price evidence quality.
- The frontend router is simple state-based routing; deep links and browser history are not mature.

## Near-Term Build Order

1. Finish product metadata completeness for Pokemon.
2. Backfill existing Pokemon rows by set.
3. Add set/product coverage reporting.
4. Add provider coverage/no-data reasons.
5. Tighten confidence scoring around sold comps and source freshness.
6. Add outlier filtering for marketplace highs and suspicious listings.
7. Improve Deal Scout filters, rows, and profitability scoring.
8. Start One Piece metadata and pricing provider strategy.
