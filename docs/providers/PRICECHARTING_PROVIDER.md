# PriceCharting Provider Notes

Last updated: 2026-06-17

## Role In DealFinder

PriceCharting is a strong candidate for the paid Pokemon price provider because its product payload is oriented around resale pricing:

- Ungraded / loose price.
- Graded price.
- PSA 10 style field through `manual-only-price` for card categories.
- BGS 10 / CGC 10 / SGC 10 style condition fields where available.
- Retail buy/sell reference fields.
- Yearly sales volume.

This is valuable for DealFinder because the business question is not only "what is the market price?" but also "is there enough upside after fees and is the card liquid enough to bother with?"

## Current Limitation

The public PriceCharting API documentation says historical price data and historical sales data are not included in the API response. That means PriceCharting is best used as a current valuation and yearly-volume provider, while our own app should snapshot prices over time if we want charts.

The CSV export includes `sales-volume`, which we treat as real yearly unit volume. The app derives estimated 30-day and 90-day volume from that annual value using simple pacing math. Those estimates are useful for sorting and rough liquidity checks, but they are not a substitute for true recent sold comps.

## Implemented Local Endpoint

```text
GET /api/productdetails/pricecharting
GET /api/scan/pricecharting?category=pokemon-cards&grade=psa10&min=10&max=200&limit=100
```

The endpoint currently:

- Returns a one-card payload for Mega Lopunny ex, Phantasmal Flames #128.
- Uses Dittobase/reference values when no PriceCharting token is present.
- Can call PriceCharting if `PRICECHARTING_TOKEN` or `PRICECHARTING_API_TOKEN` is configured.

## Current Reference Values

## Live Mega Lopunny ex Test

Tested with the paid API token on 2026-06-16.

Resolved product:

- Product ID: `11069008`
- Product Name: `Mega Lopunny ex #128`
- Console: `Pokemon Phantasmal Flames`
- Genre: `Pokemon Card`
- TCG ID: `662190`
- Release Date: `2025-11-14`

Live values returned by `/api/product`:

- Ungraded / loose: `$19.85`
- New: `$19.50`
- Complete in box: `$13.50`
- Box only: `$23.25`
- Grade 9: `$20.12`
- PSA 10: `$74.30`
- BGS 10: `$135.63`
- CGC 10: `$23.59`
- SGC 10: `$45.00`
- Retail loose buy/sell: `$6.60` / `$21.99`
- Retail new buy/sell: `$6.30` / `$20.99`
- Retail CIB buy/sell: `$3.80` / `$14.99`
- Yearly sales volume: `1,581`

The local productdetails endpoint parses 17 price rows and returns all 25 raw fields from the API response for inspection.


## Scan Screen

The `/scan` page uses PriceCharting's custom CSV export for `pokemon-cards` instead of calling one product at a time. The CSV export returns dollar-formatted prices and includes the same useful card fields:

- `loose-price` for ungraded/raw.
- `graded-price` for grade 9 style values.
- `manual-only-price` for PSA 10 style values.
- `bgs-10-price`, `condition-17-price`, and `condition-18-price` for other graded condition fields where available.
- `sales-volume` as yearly volume.

The first scan defaults to PSA 10 Pokemon cards with a market value between `$10` and `$200`. Results are sorted by yearly sales volume first, then price, so we can start looking at liquid cards before adding live buy-listing evidence.

Volume fields exposed by the scan result:

- `salesVolume`: PriceCharting yearly unit volume from the CSV.
- `estimatedThirtyDayVolume`: `salesVolume * 30 / 365`, rounded to one decimal place.
- `estimatedNinetyDayVolume`: `salesVolume * 90 / 365`, rounded to one decimal place.

If we need true 30-day / 90-day sell-through instead of estimates, we need to ingest sold listing data from another source or build a compliant scraper/import path for sold-listing pages.

## eBay Enrichment

The `/scan` page can now enrich the top PriceCharting candidates with active eBay listings through Zyte:

```text
GET /api/scan/pricecharting?category=pokemon-cards&grade=psa10&min=10&max=200&limit=10&includeEbay=true
```

Current behavior:

- Defaults the UI scan limit to 10 candidates to control Zyte usage.
- Runs separate searches for lowest buy-now and lowest auction results.
- Uses rendered Zyte `browserHtml` for eBay because direct local requests return 403 and Zyte `httpResponseBody` does not expose the rendered listing fields.
- Requires PSA 10 in the listing title for PSA 10 scans.
- Rejects obvious non-matches such as PSA 9, BGS, CGC, SGC, raw, ungraded, lots, bundles, proxies, customs, reprints, digital, and metal cards.
- Calculates the displayed eBay value as effective buy price: listing price plus detected shipping/delivery.
- Returns per-product execution stats for each eBay mode: listing blocks seen, parsed listings, matched listings, title/price/url rejection counts, match-rule rejection counts, cache usage, and error text.

The scan UI now has two listing tabs: lowest buy-now price and lowest auction. Each candidate row shows the selected listing plus the eBay execution summary, for example `66 seen / 64 parsed / 61 matched`.

This is an active-listing opportunity signal only. It should not replace sold-comps confidence once we add a sold-data source.

## Auction Scan

The `/auctionscan` page is a separate short-horizon auction scout. It uses PriceCharting first to choose candidate products, then Zyte/eBay to inspect ending-soon auction listings.

Default filters:

- Category: `pokemon-cards`, which is treated as the English Pokemon card pool.
- Grade: PSA 10 via PriceCharting `manual-only-price`.
- Market / auction range: `$1` to `$250`.
- Minimum yearly volume: `0` by default. Volume is shown for context, but it no longer gates auction discovery unless the filter is set manually.
- Candidate pool: `10` high-volume products by default.
- Result cap: `10` auction rows.
- Auction window: `6` hours by default. The fallback window is also `6` hours unless changed manually.

Returned rows include PriceCharting market value, yearly volume, estimated 30-day volume, current auction cost, time left, lowest matched buy-now listing, spread to market, spread to buy-now, and parser stats. The scan rejects obvious non-English titles such as Japanese, Korean, Chinese, German, French, Spanish, Italian, Thai, Indonesian, and Portuguese.

Smoke test note: a two-candidate 30-minute scan expanded to 60 minutes and found no rows, while a 24-hour probe found valid ending-time parsing (`3h 59m`). The default was then widened to 6 hours and the volume gate was disabled by default to improve discovery.
## Next Step
Persist PriceCharting snapshots separately from catalog records. PriceCharting does not provide historical price/sales series in the API, so charts should come from our own scheduled snapshots.
## Bulk Snapshot Job

The worker now supports a terminal-first bulk import:

```powershell
dotnet run --project src/P2W.DealFinder.Worker -- pricecharting-import --category pokemon-cards --dry-run
dotnet run --project src/P2W.DealFinder.Worker -- pricecharting-import --category pokemon-cards
```

Default behavior:

- Downloads the PriceCharting custom CSV once for the category.
- Keeps likely English Pokemon rows by default.
- Requires the PSA 10 field (`manual-only-price`) by default.
- Writes product identity rows to `dbo.PriceChartingProducts`.
- Writes timestamped valuation rows to `dbo.PriceChartingPriceSnapshots`.
- Tracks each run in `dbo.PriceChartingImportRuns`.

Important nuance: `pokemon-cards` is broader than modern Pokemon TCG singles. It can include products such as Topps, Burger King/KFC, promos, and other card-adjacent Pokemon collectibles. That is useful for a flexible deal finder, but SSMS review should decide which slices feed each sniper profile.

Cost strategy:

1. Run PriceCharting CSV import on a schedule or manually during low-attention time.
2. Use SQL filters for PSA 10 price range, volume, category/set, and trend candidates.
3. Spend Zyte/eBay calls only on the narrowed candidate set.
4. Store our own snapshots because PriceCharting's documented API does not provide historical price/sales series.

SQL review file: `docs/sql/queries/06_pricecharting_snapshot_exploration.sql`.

