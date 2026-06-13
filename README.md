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
tests/
  P2W.Cards.Tests
client/
  src/
```

Controllers are intentionally thin. Business workflows live behind application interfaces. EF Core, providers, current user handling, and background jobs live in infrastructure.

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
http://localhost:5087
```

## Run Backend

Update `src/P2W.Cards.Api/appsettings.json` if your SQL Server connection differs:

```json
"DefaultConnection": "Server=DESKTOP-7PB2QJL\\PRODSQLSERVER;Database=P2WCardsDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

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

The frontend calls the API at `http://localhost:5087` by default. If Visual Studio chooses a different API port, create `client/.env.local`:

```env
VITE_API_BASE_URL=http://localhost:YOUR_API_PORT
```

Then restart `npm run dev`.

## Tests

```powershell
dotnet test P2W.Cards.sln
```

The current suite covers search, game filters, mock providers, listing refresh and de-dupe, condition normalization, price snapshot math, watchlist behavior, alerts, disabled providers, and provider registry behavior.

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
- No real catalog import job yet; import run/error tables are ready for that future workflow.
- Seller inventory accepts image URLs, but no file upload/storage pipeline exists yet.
- Real marketplace/catalog providers are placeholders until official credentials/access are configured.
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
