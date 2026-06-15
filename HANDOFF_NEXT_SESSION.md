# P2W Cards Next Session Handoff

Current date: 2026-06-15.

Copy this document into a future Codex session if the current session/task history is lost.

## Current Objective

We are building P2W Collectibles into a marketplace intelligence tool for trading cards and collectibles.

The current focus is not checkout. The current focus is:

- Reliable catalog data by game, set, card, product, and variant.
- Real market aggregation from approved APIs.
- Set-level dashboards.
- Product pricing pages.
- Deal scouting to identify profitable inventory buys.

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
2. `DOCUMENTATION_INDEX.md`
3. `SYSTEM_DESIGN.md`
4. `APPLICATION_WIREFRAMES.md`
5. `PRODUCT_DATA_COMPLETENESS_PLAN.md`
6. `MARKETPLACE_AGGREGATION_CONTEXT.md`
7. `PRODUCT_TODO.md`
8. `CHANGELOG.md`
9. `CONFIGURATION_AND_SECRETS.md`

## Major Current Features

Backend:

- ASP.NET Core API with EF Core.
- Canonical catalog model around `CatalogProduct`.
- Catalog imports for PokemonTCG and Scryfall.
- External product mappings.
- Market aggregation tables and services.
- eBay active listing provider scaffold/implementation.
- JustTCG reference provider scaffold/config.
- PokemonTCG reference price provider.
- Market diagnostics trail.
- Market summary, comparison, chart, rankings, set dashboard, deals, provider health, and watchlist intelligence endpoints.
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

- Existing imported Pokemon rows need reimport/backfill for descriptions and corrected variants.
- Product detail metadata remains shallow.
- One Piece catalog/image source is not truly solved yet.
- Sold comps are not available/reliable yet.
- eBay active listings need stronger title matching and outlier filtering.
- Deal signals can be under-market but not profitable after fees.
- Market confidence can look too high when sold comps are absent.
- Search/global nav is still mostly scaffolded.
- Cart/login/signup/checkout are scaffolds.
- Admin pages are visible in the main nav.
- The frontend uses simple state routing instead of deep links/browser history.

## Recommended Next Steps

1. Restart backend/frontend to pick up new DTO and config changes.
2. Confirm `src/P2W.Cards.Api/appsettings.Local.json` exists locally and is ignored.
3. Run build/test.
4. Reimport or backfill Pokemon set metadata for one set first.
5. Confirm Dawn `#118` now gets `holofoil` variant and rules description after backfill.
6. Add product/set completeness metrics to the UI.
7. Add provider coverage/no-data reasons.
8. Reduce market confidence when no sold comps exist.
9. Add eBay outlier filtering and match explanation.
10. Improve Deal Scout into the primary inventory-buying workflow.

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
