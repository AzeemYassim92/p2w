# Deal Finder MVP Implementation Plan

Date: 2026-06-16

## Current State

`dealfinder` now has a clean .NET scaffold with domain entities, a scoring service*(I'm very weird with us about guesstimating numbers, what is this scoring and is it worth implementing), provider/persistence ports, and terminal command shells. It does not yet have EF Core, provider wiring, database migrations, copied Pokemon catalog rows, or a frontend.

That is intentional. The next work should validate data flow and scoring from the terminal before adding screens.

## Phase 0 - Foundation Scaffold

Status: started.

Tasks:

- [x] Create new solution in `C:\Repos\2026\ecom\dealfinder`.
- [x] Add Domain/Application/Infrastructure/Worker projects.
- [x] Add product catalog, market evidence, and deal entities.
- [x] Add deal-scoring service with fee, shipping, margin, ROI, confidence, and liquidity checks.
- [x] Add terminal command shells.
- [x] Add architecture and implementation docs.
- [ ] Add unit test project once package restore strategy is confirmed.
- [ ] Initialize git and checkpoint if this folder should become its own repo.

## Phase 1 - Persistence And Catalog Bridge

Goal: make Pokemon catalog rows available without launching the old UI.

Options:

1. Read-only bridge to old `ecompt2` database for catalog exploration.
2. One-time export/import into the new schema.
3. Port the PokemonTCG catalog sync job and write directly to the new schema.

Recommended first move: use a one-time export/import for Pokemon products and identifiers. It gives the new project a stable local dataset while avoiding accidental writes into the old prototype.

Tasks:

- [ ] Choose SQL Server connection strategy.
- [x] Add a first SQL Server bridge without forcing EF Core yet.
- [x] Create minimal catalog tables from the terminal bridge when the target DB is missing.
- [x] Build `import-set --game pokemon --set "Chaos Rising"` bridge command.
- [x] Add `coverage --game pokemon` real output for imported dealfinder catalog rows.
- [x] Validate Chaos Rising import: 122 products, 244 variants, 122 identifiers, 0 missing images, 74 missing descriptions.


Phase 1 command shape:

```powershell
dotnet run --project src/P2W.DealFinder.Worker -- import-set --game pokemon --set "Chaos Rising" --dry-run
dotnet run --project src/P2W.DealFinder.Worker -- import-set --game pokemon --set "Chaos Rising"
```

By default the command reads the old `ecompt2` catalog connection locally and creates/uses `P2WDealFinderDb` on the same SQL Server. Override with `--source-connection`, `--target-connection`, `DEALFINDER_SOURCE_CONNECTION`, or `DEALFINDER_TARGET_CONNECTION`.
## Phase 2 - Provider Observations

Goal: every external call leaves a clear trail.

Tasks:

- [ ] Add local session log folder for the new app.
- [ ] Add `ProviderObservation` writes for every provider attempt.
- [ ] Port provider diagnostics style from the old app.
- [ ] Store query text, returned count, accepted count, rejected count, no-data reason, and errors.
- [ ] Add SQL queries for blocked coverage.

## Phase 3 - Active Listings

Goal: capture eBay active listings as opportunity input, not final market value.

Tasks:

- [ ] Port eBay Browse auth/client carefully.
- [ ] Port and tighten search query builder.
- [ ] Port normalizer with stricter exclusion reasons.
- [ ] Store active listings and raw provider observation summary.
- [ ] Prevent active-listing-only data from creating high market confidence.

## Phase 4 - Reference Prices And Snapshots

Goal: compute market snapshots with honest confidence.

Tasks:

- [ ] Port JustTCG reference provider.
- [ ] Port PokemonTCG reference provider only for products where it returns usable price keys.
- [ ] Compute expected market value basis.
- [ ] Save `MarketSnapshot` rows.
- [ ] Make `scan-set` show products with missing evidence, not only successes.

## Phase 5 - Sold Comps And Liquidity

Goal: make deal confidence trade-backed.

Known issue: current eBay Browse API active listing access does not provide sold comps. We need an approved source, a licensed feed, manual CSV import, Terapeak-style export, marketplace sales from our own inventory later, or another compliant provider.

Tasks:

- [ ] Decide sold-comp source.
- [ ] Add sold comp import/provider adapter.
- [ ] Compute sold count, sell-through proxy, average daily sold, and liquidity score.
- [ ] Make profiles decide whether sold comps are required for actionability.

## Phase 6 - Deal Candidate Generation

Goal: create useful buy opportunities.

Tasks:

- [ ] Build `scan` to refresh configured source products.
- [ ] Build `scan-set` to refresh one set and write candidates.
- [ ] Write `DealCandidate` rows with explanation and risk flags.
- [ ] Build `explain` to show formula details.
- [ ] Build `mark` to record watch/buy/reject decisions.
- [ ] Add SQL query for candidates by margin, ROI, confidence, and decision.

## Phase 7 - Narrow Review UI

Goal: one useful review surface after terminal flow works.

Do not build marketplace navigation, cart, checkout, account, or storefront features here.

Screen requirements:

- Filters: game, set, buy range, net margin, ROI, confidence, liquidity, source, status.
- Rows: listing image/title, source price/shipping, matched product, identity confidence, expected market value, fees, net profit, ROI, liquidity, risk flags, explanation.
- Actions: watch, reject, purchased, open source listing, open product evidence.

## Near-Term Next Steps

1. Run `dotnet build` and `score-sample`.
2. Decide whether to initialize `dealfinder` as a separate git repo now.
3. Decide how to seed Pokemon catalog data into the new project.
4. Add EF Core and the first migration only after table names are reviewed.
5. Port eBay active listings after catalog data is queryable.
6. Continue treating sold comps as the biggest confidence blocker.


