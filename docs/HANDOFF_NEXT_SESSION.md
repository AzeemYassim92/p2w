# P2W Cards Next Session Handoff

Current date: 2026-06-15.

Copy this document into a future Codex session if the current session/task history is lost.

## Current Objective

We built P2W Collectibles as a marketplace intelligence/ecom prototype for trading cards and collectibles.

Important pivot:

- The ecom/marketplace work is still useful, but it is no longer the immediate MVP.
- This repo should be treated as the marketplace/ecom prototype and reference implementation.
- The next MVP should focus on a single-user deal finder/sniper workflow that can generate small resale profits before we scale into public marketplace features.
- Keep the catalog, provider, logging, SQL, and Pokemon sample-data lessons from this repo.
- Defer cart, checkout, signup/login, public seller storefronts, and shipping workflows until inventory volume and fulfillment strategy are clearer.

The durable product direction is:

- Product catalog is its own foundation: identity, metadata, variants, identifiers, and provider mappings.
- Market evidence is separate: current listings, reference prices, sold comps, source, match confidence, refresh status, and raw payloads.
- Deal finder is the money engine: buy opportunities in the `$25-$250+` range, at least `10%` net margin after fees/shipping, and enough liquidity/turnover to justify attention.
- Start with Pokemon cards because we have usable imported data, then expand the same product/evidence/deal model into hardware, tech, collectibles, and other resale categories.

The current repo's active focus before pivot was:

- Reliable catalog data by game, set, card, product, and variant.
- Real market aggregation from approved APIs.
- Set-level dashboards.
- Product pricing pages.
- Deal scouting to identify profitable inventory buys.

The next project should avoid broad marketplace expansion until the deal finder proves value.

## Current Repo

```text
C:\Repos\2026\ecompt2
```

Remote:

```text
https://github.com/AzeemYassim92/p2w.git
```

Current branch was `main` at the time of this handoff.

## Local Run Commands

Backend:

```powershell
dotnet run --project src/P2W.Cards.Api
```

Frontend:

```powershell
cd client
npm run dev
```

Build/test checks used most recently:

```powershell
dotnet build --no-restore -c Release
dotnet test --no-build --no-restore -c Release
cd client
npm run build
```

Catalog sync and validation:

```powershell
dotnet run --project src/P2W.Cards.Worker.Aggregation -- sync-pokemon-catalog --batch-size 5000
dotnet run --project src/P2W.Cards.Worker.Aggregation -- sync-onepiece-catalog --batch-size 5000
dotnet run --project src/P2W.Cards.Worker.Aggregation -- validate-catalog pokemon
dotnet run --project src/P2W.Cards.Worker.Aggregation -- validate-catalog one-piece
```

Local diagnostics:

```text
logs/session.log
```

The API recreates this file each local launch. It logs API startup, frontend health checks, frontend request/response timing, API request timing, catalog imports, Pokemon metadata backfills, market refresh diagnostics, provider calls, and eBay listing normalization counts. It is ignored by git.

Useful diagnostics endpoints:

```text
GET  /api/diagnostics/health
GET  /api/diagnostics/session
GET  /api/catalog/maintenance/completeness?gameSlug=pokemon
POST /api/catalog/maintenance/pokemon/backfill
```

Known local API/frontend URLs:

```text
http://127.0.0.1:5087
https://localhost:7263
http://127.0.0.1:5173
http://127.0.0.1:5174
```

## Secret Handling

Do not commit provider credentials.

Tracked `src/P2W.Cards.Api/appsettings.json` should contain blank provider credentials. The local machine may have:

```text
src/P2W.Cards.Api/appsettings.Local.json
```

That file is ignored by git and contains local provider overrides.

The API startup now loads:

- `appsettings.Local.json`
- `appsettings.{Environment}.local.json`

## Current Documentation Set

Read these first:

1. `README.md`
2. `docs/DOCUMENTATION_INDEX.md`
3. `docs/SYSTEM_DESIGN.md`
4. `docs/APPLICATION_WIREFRAMES.md`
5. `docs/PRODUCT_DATA_COMPLETENESS_PLAN.md`
6. `docs/CATALOG_SYNC_RUNBOOK.md`
7. `docs/sql/README_SQL_RUNBOOK.md`
8. `docs/MARKETPLACE_AGGREGATION_CONTEXT.md`
9. `docs/PRODUCT_TODO.md`
10. `docs/CHANGELOG.md`
11. `docs/CONFIGURATION_AND_SECRETS.md`

For a new deal-finder project/session, also read:

1. `docs/sql/queries/01_catalog_exploration.sql`
2. `docs/sql/queries/02_import_quality.sql`
3. `docs/sql/queries/03_market_aggregation_exploration.sql`
4. `src/P2W.Cards.Infrastructure/Providers/Ebay/EbayActiveListingProvider.cs`
5. `src/P2W.Cards.Infrastructure/Providers/PokemonTcg`
6. `src/P2W.Cards.Infrastructure/Services/CatalogImportServices.cs`
7. `src/P2W.Cards.Infrastructure/Services/MarketServices.cs`

## Major Current Features

Backend:

- ASP.NET Core API with EF Core.
- Canonical catalog model around `CatalogProduct`.
- Catalog imports for PokemonTCG and Scryfall.
- Terminal catalog sync/validation jobs for PokemonTCG and One Piece official catalog data.
- External product mappings.
- Market aggregation tables and services.
- eBay active listing provider scaffold/implementation.
- JustTCG reference provider scaffold/config.
- PokemonTCG reference price provider.
- Market diagnostics trail.
- Local session diagnostics in `logs/session.log`.
- Market summary, comparison, chart, rankings, set dashboard, deals, provider health, and watchlist intelligence endpoints.
- Catalog maintenance endpoints for Pokemon completeness and metadata backfill.
- Background aggregation worker project.

Frontend:

- Black/gold shell and nav.
- Marketplace home with banner, game tabs, category tiles, product cards, latest/upcoming sets, provider readiness.
- Product detail page modeled after TCGplayer.
- Separate product pricing page for market intelligence.
- Market rankings page with set insights, charts, rankings, and set catalog rows.
- Deal scanner page.
- Set dashboard page.
- Provider health page.
- Catalog watchlist intelligence page.
- Admin import and mapping pages.
- Error boundary for render failures.

## Most Recent Validation

Sample product checked:

```text
Dawn
Pokemon
Phantasmal Flames
Card #118
PokemonTCG external id: me2-118
```

Validated:

- Local set has 130 products.
- Local set product rows have images and rarity populated.
- PokemonTCG confirms the identity and core metadata.
- PokemonTCG reference price exists for `holofoil`, market around `$6.60`.
- eBay active listings exist.
- Sold comps are still missing.

Conclusion:

- Product identity can be correct while market evidence remains incomplete.
- Confidence must separate identity confidence from market confidence.
- Sold comps are the next big requirement for trustworthy deal scoring.

## Recent Code Changes To Preserve

- `MarketRankingsPage` moved set catalog listings below analytics.
- Set dashboard DTO/API now returns all products in `Products`.
- Frontend uses full `dashboard.products` before fallback merging top lists.
- PokemonTCG normalizer now pulls rules into `CatalogProduct.Description`.
- PokemonTCG variants now prefer TCGplayer price keys over generic guesses.
- API loads ignored local config overrides.
- Tracked appsettings was sanitized so provider keys are not pushed.
- Added docs: system design, wireframes, handoff, changelog, docs index, config/secrets.

## Known Issues

- Scope has become too broad for the immediate business MVP.
- The app mixes marketplace/ecom screens with deal-finder intelligence, which makes the product harder to reason about.
- The next MVP should separate product catalog, market evidence, and deal decisions more cleanly.
- Existing imported Pokemon rows need reimport/backfill for descriptions and corrected variants.
- Product detail metadata remains shallow.
- One Piece catalog/image source is not truly solved yet.
- One Piece now has an official catalog import path, but richer product metadata and market pricing still need a real source strategy.
- Sold comps are not available/reliable yet.
- eBay active listings need stronger title matching and outlier filtering.
- Deal signals can be under-market but not profitable after fees.
- Market confidence can look too high when sold comps are absent.
- Search/global nav is still mostly scaffolded.
- Cart/login/signup/checkout are scaffolds.
- Admin pages are visible in the main nav.
- The frontend uses simple state routing instead of deep links/browser history.

## Recommended Next Steps

For this repo:

1. Preserve this repo as the ecom/marketplace prototype and source of reusable code.
2. Push or otherwise checkpoint the current work before making a new project.
3. Use `docs/sql/README_SQL_RUNBOOK.md` to understand the imported catalog and market data from SSMS.
4. Run catalog validation only when needed; do not keep expanding public marketplace features until the deal finder MVP is clear.
5. When returning to ecom, resume with inventory, shipping, seller workflow, checkout, and public marketplace concerns.

For the next deal-finder MVP:

1. Prefer a new sibling project folder, for example `C:\Repos\2026\p2w-dealfinder`, to avoid dragging the prototype's complexity into the new app.
2. Reuse concepts and selected code from this repo, not the whole app structure.
3. Start with three bounded areas: Product Catalog, Market Evidence Store, Deal Finder.
4. Start with Pokemon card catalog data because it gives us a validated dataset and real provider lessons.
5. Make eBay active listings the first opportunity source.
6. Treat sold comps/liquidity as a first-class missing-data problem, not a UI afterthought.
7. Use default opportunity filters: buy range `$25-$250+`, minimum `10%` net margin after fees/shipping, minimum profit dollars, high identity confidence, and sufficient turnover.
8. Keep it local-first and single-user until the workflow proves it can identify profitable inventory.
9. Avoid cart/login/checkout/seller storefronts in the deal-finder MVP.
10. Keep SQL transparency and terminal jobs as first-class tools.

If continuing inside this repo instead of a new sibling folder, create a clean subfolder/app boundary such as:

```text
dealfinder-mvp/
```

and avoid wiring it into the existing marketplace UI until its data model and deal workflow are stable.

## Safe Push Checklist

Before pushing:

```powershell
git status -sb
git diff -- src/P2W.Cards.Api/appsettings.json
git check-ignore -v src/P2W.Cards.Api/appsettings.Local.json
```

Confirm:

- No real keys in `appsettings.json`.
- `appsettings.Local.json` is ignored.
- `dotnet build --no-restore -c Release` passes.
- `dotnet test --no-build --no-restore -c Release` passes.
- `cd client; npm run build` passes.

If GitHub CLI auth is expired:

```powershell
gh auth login -h github.com
```
