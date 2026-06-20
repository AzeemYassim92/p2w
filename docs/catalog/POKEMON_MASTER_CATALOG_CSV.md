# Pokemon Master Catalog CSV

Last updated: 2026-06-17

## Purpose

The master catalog CSV is the local product identity spine for Pokemon deal finding. It is intentionally reviewable outside the app before it becomes SQL state.

PriceCharting is currently the seed source because its paid CSV export gives us broad Pokemon card/product coverage, useful provider IDs, pricing fields, and yearly volume in one lightweight pull.

## Commands

Build the reviewable CSV:

```powershell
dotnet run --project src/P2W.DealFinder.Worker -- pokemon-catalog-build --category pokemon-cards --output data/generated/pokemon_master_catalog.csv
```

Dry-run the import:

```powershell
dotnet run --project src/P2W.DealFinder.Worker -- pokemon-catalog-import --csv data/generated/pokemon_master_catalog.csv --dry-run
```

Import into SQL:

```powershell
dotnet run --project src/P2W.DealFinder.Worker -- pokemon-catalog-import --csv data/generated/pokemon_master_catalog.csv
```

Useful build switches:

- `--limit 500`: build/import a bounded sample.
- `--require-psa10`: keep only rows with PriceCharting PSA 10 values.
- `--include-non-english`: keep non-English rows instead of filtering them out.

## Current Full Build

The first full English build from `pokemon-cards` produced:

- Provider rows: `88,474`.
- Master catalog rows: `44,987`.
- Likely Pokemon TCG rows: `40,637`.
- Rows with PSA 10 value: `37,556`.
- Skipped non-English/non-Pokemon-signal rows: `43,487`.

The generated CSV is ignored by git under `data/generated/`.

The full import has been verified into `dbo.PokemonMasterCatalog` with `44,987` rows. The observed row-by-row import took roughly 12 minutes locally, so a future optimization should replace it with a staging-table bulk merge before we scale beyond this dataset.

## Identity Fields

Core identity:

- `CatalogKey`: local deterministic key, currently `pokemon|pricecharting|{PriceChartingProductId}`.
- `Game`
- `ProductType`
- `ProductFamily`
- `Language`
- `IsLikelyPokemonTcg`
- `ReleaseDate`
- `SetName`
- `SetCode`
- `CardName`
- `CardNumber`
- `VariantName`
- `Rarity`

Provider mapping:

- `PriceChartingProductId`
- `PriceChartingProductName`
- `PriceChartingConsoleName`
- `PriceChartingCategory`
- `PriceChartingProductUrl`
- `PriceChartingSearchUrl`
- `TcgPlayerId`
- `Upc`

Valuation/liquidity snapshot:

- `UngradedPrice`
- `Grade9Price`
- `Psa10Price`
- `Bgs10Price`
- `Cgc10Price`
- `Sgc10Price`
- `SalesVolumeYearly`
- `EstimatedThirtyDayVolume`
- `EstimatedNinetyDayVolume`
- `SourceCapturedAtUtc`

Search support:

- `EbayPsa10SearchQuery`
- `RawPriceChartingJson`

## Known Data Caveats

PriceCharting's `pokemon-cards` category is broader than modern Pokemon TCG singles. It includes card-adjacent collectibles such as KFC, Topps, Artbox, Sealdass, and other non-core products. That is not bad for a flexible deal finder, but we need scanner profiles:

- Core Pokemon TCG.
- Card-adjacent Pokemon collectibles.
- English only.
- PSA 10 only.
- High-volume only.
- Target buy range.

Some provider release dates are clearly invalid for Pokemon. The CSV builder normalizes dates before `1996-01-01` or more than two years in the future to blank so bad dates sort to the bottom.

## SQL Tables

Import creates/upserts:

- `dbo.PokemonMasterCatalogImports`
- `dbo.PokemonMasterCatalog`

Use `docs/sql/queries/07_pokemon_master_catalog_exploration.sql` for SSMS review.


