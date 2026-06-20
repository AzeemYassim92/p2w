# Deal Finder MVP Architecture

Date: 2026-06-16

## 1. Existing State Summary

The old `ecompt2` app is valuable as a reference implementation, but it is too broad for the immediate MVP. It contains a working Pokemon catalog import path, One Piece catalog experiments, eBay active listing integration, JustTCG and PokemonTCG reference price work, local session logging, SQL exploration scripts, and a useful TCGplayer-inspired pricing page.

Reusable concepts:

- `CatalogProduct`, product variants, external provider mappings, import runs, and catalog validation jobs.
- Market evidence separation between reference prices, active listings, sold comps, snapshots, metrics, and provider diagnostics.
- eBay active listing normalization and exclusion ideas, especially keyword exclusions and match confidence.
- Local-first terminal jobs and SQL transparency.
- The lesson that identity confidence is not market confidence.

Avoid or quarantine:

- Cart, checkout, signup, login, seller storefront, public marketplace browsing, and broad frontend dashboards.
- Product cards and public marketplace pages as the primary workflow.
- Active-listing-only confidence inflation.
- Mixing catalog rows, market observations, and deal decisions into one UI or one table family.

## 2. Repo And Folder Strategy

The new work should live in `C:\Repos\2026\ecom\dealfinder`. The old app now lives beside it at `C:\Repos\2026\ecom\ecompt2` and should be treated as a reference and data source, not as the direct implementation target.

This gives us a clean bounded project while keeping the old source close enough to copy proven ideas. The first pass should not pull in the old React frontend or all old EF entities. We should move only concepts that support the deal-finder MVP.

## 3. Simplified Architecture

The MVP has exactly three top modules.

### A. Product Catalog

Question: what is this product?

Responsibilities:

- Store stable product identity, metadata, variants, and provider identifiers.
- Support Pokemon cards first, then sealed products, tech, hardware, and broader collectibles.
- Keep catalog completeness and provider coverage visible.
- Never decide whether something is a good deal.

Core records:

- `CatalogProduct`
- `ProductVariant`
- `ProductIdentifier`
- `CatalogProductProviderCoverage`

### B. Market Evidence Store

Question: what evidence do we have about market value and liquidity?

Responsibilities:

- Store raw-ish normalized evidence from providers.
- Keep reference prices, active listings, sold comps, snapshots, and provider observations separate.
- Preserve no-data reasons, query text, match confidence, and exclusion reasons.
- Make stale or missing evidence obvious.

Core records:

- `MarketplaceSource`
- `ActiveListing`
- `SoldComp`
- `ReferencePrice`
- `MarketSnapshot`
- `ProviderObservation`

### C. Deal Finder/Sniper

Question: should I consider buying this opportunity, and why?

Responsibilities:

- Score listing opportunities against configured buy profiles.
- Prefer sold-comp backed expected values.
- Estimate all costs before calling a deal actionable.
- Save the explanation, risk flags, and human decision history.

Core records:

- `FeeProfile`
- `DealSearchProfile`
- `DealCandidate`
- `DealDecisionHistory`

## 4. Initial DB Model

The first schema should be boring and queryable. Proposed table groups:

Product catalog:

- `CatalogProducts`: identity, category, game/brand, set, card number, rarity, metadata, image, active flag.
- `ProductVariants`: foil, reverse holo, language, printing, graded fields.
- `ProductIdentifiers`: provider IDs and identity confidence.
- `CatalogProductProviderCoverage`: per-product provider coverage status.

Market evidence:

- `MarketplaceSources`: eBay, PokemonTCG, JustTCG, future sources.
- `ProviderObservations`: every refresh attempt and no-data reason.
- `ActiveListings`: current listings with effective buy price and match/exclusion fields.
- `SoldComps`: completed sales when a real source exists.
- `ReferencePrices`: provider reference values.
- `MarketSnapshots`: computed product value and liquidity at a point in time.

Deal finder:

- `FeeProfiles`: marketplace fee, payment fee, outbound shipping, packing, buffer.
- `DealSearchProfiles`: thresholds such as buy range and minimum net margin.
- `DealCandidates`: scored opportunities.
- `DealDecisionHistory`: manual watch/buy/reject notes.

## 5. Deal Scoring Model

Inputs:

- Listing price before inbound shipping.
- Inbound shipping charged by the seller.
- Expected market value.
- Market value basis.
- Identity confidence.
- Market confidence.
- Liquidity score.
- Active listing count.
- Sold comp count.
- Evidence freshness.
- Fee profile.
- Search thresholds.

Formula:

```text
ListingPrice = item price before inbound shipping
InboundShippingPrice = shipping charged by seller
EffectiveBuyPrice = ListingPrice + InboundShippingPrice

EstimatedSaleFees = ExpectedMarketValue * FeePercent + FixedFee
EstimatedTotalCost =
    EffectiveBuyPrice
    + EstimatedSaleFees
    + OutboundShippingCost
    + PackingCost
    + Buffer

NetProfit = ExpectedMarketValue - EstimatedTotalCost
NetMarginPercent = NetProfit / ExpectedMarketValue
ROI = NetProfit / EffectiveBuyPrice
```

The scoring service is deterministic and transparent. It must never hide the raw numbers behind a magic score. It uses two layers.

Hard filters:

- Buy range.
- Minimum profit dollars.
- Minimum margin.
- Minimum ROI.
- Listing must not be excluded.
- Identity, market confidence, liquidity, and sold-comp requirements.

Sort score:

- Profit.
- ROI.
- Liquidity.
- Confidence.
- Evidence freshness.
- Risk penalty.

The app should say why a candidate passed or failed before showing a sort score.

Expected market value rules:

1. Prefer sold-comp median.
2. Use sold-comp average only when median is unavailable but enough comps exist.
3. Use reference price as lower-confidence fallback.
4. Treat active-listing-only value as low confidence and review-only unless explicitly overridden.

A candidate is actionable only when:

- Effective buy price is in the configured buy range, default `$25-$250+`.
- Net margin is at least `10%` after fees, inbound shipping, outbound shipping, packing, fixed fee, and buffer.
- ROI meets the configured threshold.
- Net profit meets the configured dollar floor.
- Identity confidence meets the threshold.
- Market confidence meets the threshold after caps.
- Liquidity score meets the threshold.
- Sold comps exist when the profile requires trade-backed evidence.
- The listing is not excluded for lot, wrong language, graded/raw mismatch, variant mismatch, damaged condition, outlier price, suspicious title, or insufficient evidence.

Every candidate must carry raw math, hard-filter failures, sort score, explanation, and risk flags.
## 6. Existing Code Copy/Refactor Plan

Copy concepts, not whole features.

Good candidates to port after Phase 0:

- Pokemon catalog importer and normalizer, simplified around `CatalogProduct` and `ProductIdentifier`.
- eBay active listing client, search builder, and normalizer, with stricter matching and better diagnostics.
- JustTCG reference provider as a reference-price source.
- Local session logging style from old `logs/session.log`.
- SQL runbook patterns.

Do not port yet:

- React marketplace pages.
- Cart, login, signup, checkout, watchlist, seller inventory, alerts, public product browsing.
- Old market rankings UI.

## 7. Terminal-First MVP Commands

Target command shape:

```powershell
dotnet run --project src/P2W.DealFinder.Worker -- scan --profile default --dry-run
dotnet run --project src/P2W.DealFinder.Worker -- scan-set --game pokemon --set "Phantasmal Flames" --dry-run
dotnet run --project src/P2W.DealFinder.Worker -- explain --candidate-id <id>
dotnet run --project src/P2W.DealFinder.Worker -- coverage --game pokemon
dotnet run --project src/P2W.DealFinder.Worker -- mark --candidate-id <id> --decision watch
```

The first scaffold already exposes these command shells. Next work wires them to storage and providers.

## 8. SQL Transparency

Create and maintain:

```text
docs/sql/queries/04_deal_finder_exploration.sql
```

It should help answer:

- Which products are catalog-complete?
- Which products have provider coverage?
- Which products have active listings but no sold comps?
- Which candidates pass/fail thresholds?
- Which no-data reasons are blocking confidence?

## 9. UI Strategy

No large frontend in the first pass. The first UI, when needed, should be one narrow review screen:

- Filtered Deal Scout table.
- Listing title/image/price/shipping.
- Matched product and confidence.
- Expected market value and basis.
- Fees, net profit, ROI, liquidity, risk flags.
- Watch, reject, purchased actions.

## 10. Phased Implementation Plan

Phase 0: scaffold solution, docs, scoring logic, command shells.

Phase 1: persistence and catalog import/read model for Pokemon. The first bridge copies one selected set from the old catalog DB into a lean `P2WDealFinderDb` schema.

Phase 2: provider observations and active listing refresh.

Phase 3: reference price integration and market snapshots.

Phase 4: sold comp source strategy and liquidity scoring.

Phase 5: deal candidate generation and decision history.

Phase 6: narrow review UI after terminal flow proves useful.

## 11. Validation Gates

Before adding broad features:

- A set can be scanned from the terminal.
- Catalog count and provider count can be reconciled.
- Every provider refresh writes an observation.
- Active listings are matched or excluded with a reason.
- Reference prices do not produce high confidence by themselves.
- Sold-comp absence visibly blocks or downgrades actionability.
- Deal candidates show net profit, net margin, ROI, liquidity, and explanation.

## 12. Current Scaffold Output

Created:

- New solution: `P2W.DealFinder.sln`
- Domain module with catalog, evidence, and deal records.
- Application deal-scoring service.
- Provider and persistence ports.
- Terminal worker command shells.
- SQL/documentation folder structure.


