# P2W Cards

P2W Cards is an MVP trading card marketplace and price aggregation platform for Pokemon and Magic: The Gathering cards. It proves the search, listing aggregation, reference pricing, watchlist, alert, provider, and background refresh flow without checkout, payments, seller onboarding, or scraping.

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
- Pokemon cards: Charizard, Pikachu, Blastoise, Mewtwo, Gengar, Rayquaza, Lugia, Umbreon
- Magic cards: Black Lotus, Sol Ring, Lightning Bolt, Counterspell, Mox Diamond, Mana Crypt, Rhystic Study, Dockside Extortionist

## Demo Flow

1. Apply EF migrations.
2. Run the backend.
3. Run the frontend.
4. Search `Charizard` at `/cards/search`.
5. Review the 10 featured individual card records on the marketplace page.
6. Open the card detail.
7. Refresh listings.
8. Refresh reference prices and capture a listing snapshot.
9. Add the card to the watchlist.
10. Create a target price alert.
11. Review watchlist and alerts.

## Known Limitations

- No checkout, payment processing, seller onboarding, or order management.
- No website scraping.
- Real marketplace/catalog providers are placeholders until official credentials/access are configured.
- Alerts are logged and marked as triggered; email/SMS/push is intentionally out of scope.
- The frontend uses a lightweight in-memory route switch instead of a router package.

## Roadmap

- Replace placeholder providers with official API integrations.
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
