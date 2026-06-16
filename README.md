# P2W Cards

P2W Cards is an MVP trading card marketplace and price aggregation platform for trading cards. It proves the product catalog, search, listing aggregation, reference pricing, watchlist, alert, provider, and background refresh flow without checkout, payments, seller onboarding, or scraping.

## Tech Stack

- Backend: ASP.NET Core Web API on .NET 9
- Data: Entity Framework Core with SQL Server
- Architecture: Domain, Application, Infrastructure, API projects
- Frontend: React + TypeScript + Vite
- Tests: xUnit with EF Core InMemory
- API docs: ASP.NET Core OpenAPI at `/openapi/v1.json`

## Structure

```text
src/
  P2W.Cards.Api
  P2W.Cards.Application
  P2W.Cards.Domain
  P2W.Cards.Infrastructure
  P2W.Cards.Worker.Aggregation
tests/
  P2W.Cards.Tests
client/
  src/
```

Controllers are intentionally thin. Business workflows live behind application interfaces. EF Core, providers, current user handling, and background jobs live in infrastructure.

## Documentation Map

Start with `docs/DOCUMENTATION_INDEX.md` for the full doc map.

- `docs/SYSTEM_DESIGN.md`: architecture, data flow, frontend route map, and near-term technical direction.
- `docs/MARKETPLACE_AGGREGATION_CONTEXT.md`: marketplace aggregation tables, fields, API surfaces, and provider design context.
- `docs/PRODUCT_DATA_COMPLETENESS_PLAN.md`: strategy for filling product detail fields before adding more features.
- `docs/CATALOG_SYNC_RUNBOOK.md`: terminal commands for Pokemon and One Piece catalog sync/validation jobs.
- `docs/sql/README_SQL_RUNBOOK.md`: SSMS query runbook for catalog, import, and market aggregation data exploration.
- `docs/APPLICATION_WIREFRAMES.md`: text wireframes for the current application surfaces.
- `docs/HANDOFF_NEXT_SESSION.md`: paste-friendly recovery note for the next Codex/session.
- `docs/CHANGELOG.md`: what changed, where, and when.
- `docs/CONFIGURATION_AND_SECRETS.md`: local credentials and safe GitHub push practices.
- `docs/PRODUCT_TODO.md`: living product backlog.

## Part 2 Catalog Core

The catalog layer adds marketplace-ready product structure without replacing the original card/search MVP:

- Games: Magic: The Gathering, Pokemon, One Piece as primary focus games, with additional inactive/future-ready TCG rows.
- Sets: recent and upcoming set records for primary games.
- Categories: singles, sealed, booster packs, booster boxes, ETBs, decks, graded cards, accessories, supplies, deals, and related subcategories.
- Products: catalog products for individual cards, packs, boxes, decks, and sealed products.
- Variants: product-level variant scaffolding for normal, foil, promo, sealed case, and future expansion.
- Seller inventory: catalog-backed seller inventory items with condition, quantity, asking price, grading fields, and image URL rows.
- Import tracking: catalog import runs and import errors for future provider ingestion jobs.

Part 2 intentionally does not add real external imports, checkout, payments, shipping, image upload storage, AI grading, or auth changes.

## Part 3 Catalog Import Architecture

Catalog imports are provider-based. Dry runs call a provider, normalize external records, match against the existing catalog, and return create/update/skip counts before any writes happen.

- Scryfall is the first Magic metadata provider and uses official Scryfall API endpoints.
- PokemonTCG is the first Pokemon metadata provider and uses the official Pokemon TCG API.
- One Piece official card list import is available for base set/card identity and images.
- Real imports create `CatalogImportRun` records, upsert sets/products/variants, create external mappings, and write record-level `CatalogImportError` rows.
- Imports page through provider APIs and save resume checkpoints so later runs continue from the next page instead of replaying page one.
- PokemonTCG uses numeric API pages. Scryfall uses its returned `next_page` URL.
- External mappings are confidence-scored and marked `AutoMatched` or `NeedsReview`.
- Mapping review endpoints can approve, reject, and save notes.
- Catalog pricing has its own snapshot table, but only mock pricing is implemented for now.

Images are stored as URLs only. The app does not download or store image binaries.

## Catalog Sync Jobs

Populate and validate base catalogs from the terminal without launching the frontend:

```powershell
dotnet run --project src/P2W.Cards.Worker.Aggregation -- sync-pokemon-catalog --batch-size 5000
dotnet run --project src/P2W.Cards.Worker.Aggregation -- sync-onepiece-catalog --batch-size 5000
dotnet run --project src/P2W.Cards.Worker.Aggregation -- validate-catalog pokemon
dotnet run --project src/P2W.Cards.Worker.Aggregation -- validate-catalog one-piece
```

Use `--resume` with `catalog-sync pokemon` or `catalog-sync one-piece` after an interrupted run. See `docs/CATALOG_SYNC_RUNBOOK.md` for completion criteria and expected count deltas.

## Part 4 Marketplace Aggregation

The catalog-level aggregation slice attaches market data to `CatalogProduct` and `ProductVariant`, not the older MVP `Card` table.

- New market tables store marketplace sources, SKU mappings, active listings, sold comps, market snapshots, computed metrics, ingestion runs/errors/checkpoints, product view events, and catalog watchlist items.
- eBay active listing support uses the official API path only. It does not scrape.
- eBay sold comps remain disabled because approved sold/completed data access is not configured.
- PokemonTCG reference prices are used when the provider payload contains usable price data.
- JustTCG reference provider configuration has been added as an additional reference-price path.
- PriceCharting reference provider scaffolding exists.
- `MockMarket` returns deterministic labeled demo data so charts, rankings, comparison rows, and deal panels can be built before real feeds are enabled.
- `P2W.Cards.Worker.Aggregation` is a separate disabled-by-default worker. Set `MarketAggregation:Enabled=true` only when scheduled refreshes should run.

Frontend market surfaces now include product market intelligence, market rankings, deal scanner, set dashboard lookup, catalog watchlist intelligence, and provider health.

## Quick Start

Use two processes during local development:

1. Backend API from Visual Studio or `dotnet run`
2. React frontend from a terminal in `client`

The Visual Studio startup project should be:

```text
src/P2W.Cards.Api/P2W.Cards.Api.csproj
```

In Solution Explorer, right-click `P2W.Cards.Api`, choose `Set as Startup Project`, then run the `https` profile. The API should listen on:

```text
https://localhost:7263
http://localhost:5087
```

The React dev server should listen on:

```text
http://127.0.0.1:5174
```

For local React development, the client calls the API over HTTP:

```text
http://127.0.0.1:5087
```

## Run Backend

Update `src/P2W.Cards.Api/appsettings.json` if your SQL Server connection differs:

```json
"DefaultConnection": "Server=DESKTOP-7PB2QJL\\PRODSQLSERVER;Database=P2WCardsDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

Provider credentials should not be stored in tracked files. For local provider keys, copy the example local override and edit the ignored file:

```powershell
Copy-Item src/P2W.Cards.Api/appsettings.Local.example.json src/P2W.Cards.Api/appsettings.Local.json
```

See `docs/CONFIGURATION_AND_SECRETS.md` before pushing changes to GitHub.

Create/apply the database:

```powershell
dotnet ef database update --project src/P2W.Cards.Infrastructure --startup-project src/P2W.Cards.Api
```

Run the API:

```powershell
dotnet run --project src/P2W.Cards.Api
```

Open API metadata:

```text
https://localhost:7263/openapi/v1.json
```

In Visual Studio, open `P2W.Cards.sln`, set `P2W.Cards.Api` as the startup project, and start the `https` profile. Do not use the React `client` folder as the Visual Studio startup item.

## Run Frontend

```powershell
cd client
npm install
npm run dev
```

The frontend calls the API at `http://127.0.0.1:5087` by default. If Visual Studio chooses a different API port, create `client/.env.local`:

```env
VITE_API_BASE_URL=http://localhost:YOUR_API_PORT
```

Then restart `npm run dev`.

## Tests

```powershell
dotnet test P2W.Cards.sln
cd client
npm run build
```

The current suite covers search, game filters, mock providers, listing refresh and de-dupe, condition normalization, price snapshot math, watchlist behavior, alerts, disabled providers, provider registry behavior, catalog discovery, seller inventory, import previews/runs, provider normalization, mapping review, mock catalog pricing, seeded marketplace sources, eBay query/normalization, market summary demo labeling, and mock market aggregation.

## Example API Calls

```http
GET /api/marketplace/home
GET /api/marketplace/home?gameSlug=pokemon
GET /api/catalog/games?primaryOnly=true
GET /api/catalog/categories
GET /api/catalog/sets?gameSlug=magic-the-gathering
GET /api/catalog/sets?gameSlug=pokemon&upcoming=true
GET /api/catalog/products?gameSlug=one-piece&take=12
GET /api/catalog/products?categorySlug=booster-packs
GET /api/catalog/products/{catalogProductId}
GET /api/catalog/providers/capabilities
POST /api/catalog/import/preview
POST /api/catalog/import/run
GET /api/catalog/import/runs
GET /api/catalog/import/runs/{importRunId}
GET /api/catalog/mappings/review?status=NeedsReview&take=50
PATCH /api/catalog/mappings/{mappingId}/approve
PATCH /api/catalog/mappings/{mappingId}/reject
PATCH /api/catalog/mappings/{mappingId}/notes
GET /api/catalog/products/{catalogProductId}/pricing/history
POST /api/catalog/products/{catalogProductId}/pricing/refresh
POST /api/market/aggregation/products/{catalogProductId}/refresh
POST /api/market/aggregation/sets/{cardSetId}/refresh
GET /api/market/aggregation/runs
GET /api/market/products/{catalogProductId}/summary
GET /api/market/products/{catalogProductId}/confidence
GET /api/market/products/{catalogProductId}/comparison
GET /api/market/products/{catalogProductId}/chart
GET /api/market/products/{catalogProductId}/deals
GET /api/market/deals
GET /api/market/rankings/trending
GET /api/market/rankings/high-volume
GET /api/market/rankings/movers
GET /api/market/rankings/opportunities
GET /api/market/sets/{cardSetId}/dashboard
GET /api/catalog-watchlist
POST /api/catalog-watchlist
GET /api/market/providers/health
GET /api/seller-inventory
POST /api/seller-inventory
GET /api/cards/search?query=charizard&game=Pokemon
GET /api/cards/featured?productType=individual-cards&take=10
GET /api/cards/search?query=sol%20ring&game=Magic
GET /api/cards/{cardId}
GET /api/cards/{cardId}/listings
POST /api/cards/{cardId}/refresh-listings
POST /api/cards/{cardId}/price-history/refresh-reference-prices
POST /api/cards/{cardId}/price-history/capture-listing-snapshot
POST /api/watchlist
GET /api/watchlist
POST /api/alerts
GET /api/alerts
GET /api/providers
GET /api/providers/health
```

## Provider Architecture

The MVP uses this flow:

```text
External Source -> Provider Adapter -> External DTO -> Normalization -> SQL Server -> Application Service -> API -> React UI
```

Mock providers are enabled by default. Real providers are scaffolded as safe disabled placeholders for TCGplayer, eBay, Scryfall, Pokemon TCG, MTGJSON, PriceCharting, and Card Kingdom. Disabled providers return empty results and health information without throwing.

The catalog provider capability endpoint exposes where each connector is expected to fit:

- Scryfall: Magic catalog metadata and images.
- PokemonTCG: Pokemon catalog metadata and images.
- TCGplayer: catalog search, marketplace listing, and price reference candidate.
- eBay: broad marketplace listings.
- MTGJSON, PriceCharting, Card Kingdom: price/reference candidates.

To add a real provider:

1. Add an options class or extend the existing provider options.
2. Add external DTOs for the official API response.
3. Implement the correct provider interface.
4. Normalize external data into internal DTOs/entities.
5. Register the provider in `DependencyInjection`.
6. Add tests for disabled and enabled behavior.

## Catalog Import Flow

Use the Import tab in the React app for admin-style ingestion:

1. Pick the provider and import type.
2. Run a dry run first to see read/create/update/skip counts.
3. Run the import when the preview looks right.
4. Leave `Use checkpoint` on to resume from the last saved provider cursor.
5. Leave `Save next` on to store the next cursor after a successful import.

Duplicates are prevented by `ExternalProductMappings(SourceName, ExternalId)`, backed up by normalized game/set/card-number/name matching. Re-importing the same provider records should update mapped products instead of creating new catalog products.

## Seed Data

The EF model seeds:

- Marketplaces: MockMarket, eBay, TCGplayer, Card Kingdom, PriceCharting, Cardmarket
- External sources: Mock, TCGplayer, eBay, Scryfall, MTGJSON, PokemonTCG, PriceCharting, CardKingdom, Cardmarket
- Primary catalog games: Magic: The Gathering, Pokemon, One Piece
- Product categories: singles, sealed, packs, boxes, ETBs, decks, graded cards, supplies, deals, and related children
- Catalog sets and products for Magic, Pokemon, and One Piece
- Pokemon cards: Charizard, Pikachu, Blastoise, Mewtwo, Gengar, Rayquaza, Lugia, Umbreon
- Magic cards: Black Lotus, Sol Ring, Lightning Bolt, Counterspell, Mox Diamond, Mana Crypt, Rhystic Study, Dockside Extortionist

## Demo Flow

1. Apply EF migrations.
2. Run the backend.
3. Run the frontend.
4. Open the marketplace home and switch between Magic, Pokemon, and One Piece.
5. Review category tiles, trending products, featured products, latest sets, upcoming sets, and provider readiness.
6. Use Search for `Charizard` or `Sol Ring`.
7. Open the card detail.
8. Refresh listings.
9. Refresh reference prices and capture a listing snapshot.
10. Add the card to the watchlist.
11. Create a target price alert.
12. Review watchlist, alerts, and seller inventory.

## Known Limitations

- No checkout, payment processing, seller onboarding, or order management.
- No website scraping.
- Catalog imports are synchronous admin actions for now; market aggregation has a separate worker but it is disabled by default.
- Seller inventory accepts image URLs, but no file upload/storage pipeline exists yet.
- Real marketplace/catalog providers are placeholders until official credentials/access are configured.
- Market intelligence panels may show labeled deterministic demo data until real provider feeds are enabled.
- Alerts are logged and marked as triggered; email/SMS/push is intentionally out of scope.
- The frontend uses a lightweight in-memory route switch instead of a router package.

## Roadmap

- Replace placeholder providers with official API integrations.
- Build catalog import jobs for Scryfall, PokemonTCG, TCGplayer, and other approved APIs.
- Add seller inventory forms, product matching workflow, and listing review.
- Add authenticated user accounts and JWT configuration.
- Add collection tracking.
- Add richer pricing charts.
- Add affiliate links where approved.
- Add premium pricing intelligence features.
- Add AI card scanning later, outside this MVP boundary.

## Hosting Notes

For 24/7 hosting later, the clean path is:

- Host the ASP.NET Core API as a Windows Service or behind IIS on a Windows VM.
- Host SQL Server on the same VM for early MVP simplicity, or a managed SQL database when reliability matters more.
- Build the React app with `npm run build` and serve the generated `client/dist` files from IIS, Nginx, or a static site host.
- Put HTTPS in front of both frontend and API.
- Move secrets and provider API keys out of `appsettings.json` into environment variables, user secrets, Azure Key Vault, or another secrets manager.
- Add production logging, backups, database migration workflow, and health checks before leaving it online unattended.
