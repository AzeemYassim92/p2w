# P2W Deal Finder

Local-first resale intelligence MVP for finding buy opportunities before rebuilding public marketplace features.

This project is intentionally separate from the old `ecompt2` marketplace prototype. The old app remains the reference for Pokemon catalog imports, provider experiments, logging, and SQL exploration. This app starts from three bounded areas:

1. Product Catalog: what the product is.
2. Market Evidence Store: what market evidence we have.
3. Deal Finder/Sniper: whether a listing is worth considering and why.

## Current Scaffold

- `src/P2W.DealFinder.Domain`: catalog, evidence, and deal entities.
- `src/P2W.DealFinder.Application`: deal scoring and provider/persistence ports.
- `src/P2W.DealFinder.Infrastructure`: provider notes and adapters, including the first JustTCG client.
- `src/P2W.DealFinder.Worker`: terminal-first command shell.
- `src/P2W.DealFinder.Api`: local API plus static product detail and scan pages.
- `docs`: MVP architecture, implementation plan, and SQL exploration queries.

## Terminal Shape

```powershell
dotnet run --project src/P2W.DealFinder.Worker -- help
dotnet run --project src/P2W.DealFinder.Worker -- score-sample
dotnet run --project src/P2W.DealFinder.Worker -- import-set --game pokemon --set "Chaos Rising" --dry-run
dotnet run --project src/P2W.DealFinder.Worker -- coverage --game pokemon
dotnet run --project src/P2W.DealFinder.Worker -- pricecharting-import --category pokemon-cards
dotnet run --project src/P2W.DealFinder.Worker -- pokemon-catalog-build --category pokemon-cards --output data/generated/pokemon_master_catalog.csv
dotnet run --project src/P2W.DealFinder.Worker -- pokemon-catalog-import --csv data/generated/pokemon_master_catalog.csv --dry-run --dry-run
dotnet run --project src/P2W.DealFinder.Worker -- pricecharting-import --category pokemon-cards
dotnet run --project src/P2W.DealFinder.Worker -- pokemon-catalog-build --category pokemon-cards --output data/generated/pokemon_master_catalog.csv
dotnet run --project src/P2W.DealFinder.Worker -- pokemon-catalog-import --csv data/generated/pokemon_master_catalog.csv --dry-run
dotnet run --project src/P2W.DealFinder.Worker -- justtcg-games
dotnet run --project src/P2W.DealFinder.Worker -- justtcg-cards --game pokemon --name Charizard --price-history 180d --limit 3
dotnet run --project src/P2W.DealFinder.Worker -- justtcg-range --game pokemon --set "Chaos Rising" --min 15 --max 25 --price-history 180d --take 40
dotnet run --project src/P2W.DealFinder.Worker -- scan-set --game pokemon --set "Phantasmal Flames" --dry-run
dotnet run --project src/P2W.DealFinder.Api --urls http://127.0.0.1:5178
```

The commands are scaffolded first so we can wire data deliberately instead of burying behavior in a UI.
## Product Detail Pages

```text
http://127.0.0.1:5178/productdetails?source=justtcg
http://127.0.0.1:5178/productdetails?source=pricecharting
```

The JustTCG page requests one card only: Mega Lopunny ex, Phantasmal Flames #128. The PriceCharting page is built around current ungraded/graded values and uses reference values until a PriceCharting token is configured.


## PriceCharting Scan

```text
http://127.0.0.1:5178/scan
```

The first scan screen filters PriceCharting's Pokemon CSV export by grade and price range. It defaults to PSA 10 cards from $10 to $200, enriches the top 10 through Zyte/eBay, and shows lowest buy-now plus lowest auction tabs when matches are found. It displays PriceCharting yearly volume, estimated 30-day volume, and per-product eBay execution stats so we can see how many listings were seen, parsed, and matched.

```text
http://127.0.0.1:5178/auctionscan
```

The auction scan is a separate short-horizon screen for English Pokemon PSA 10 auctions. It defaults to $1-$250, no minimum yearly volume gate, 10 candidates, 10 returned rows, and auctions ending within 6 hours.
## Immediate Next Step

Validate whether JustTCG quota resets for the one-card productdetails endpoint. If we choose PriceCharting, configure `PRICECHARTING_TOKEN`, verify Mega Lopunny ex #128 live fields, then persist provider snapshots separately from catalog records.
## PriceCharting Snapshot Import

Use PriceCharting as the broad, cheap valuation backbone before spending Zyte/eBay calls. The worker downloads the `pokemon-cards` CSV once, filters to likely English Pokemon rows with PSA 10 values by default, and stores products plus timestamped price snapshots in SQL.

```powershell
dotnet run --project src/P2W.DealFinder.Worker -- pricecharting-import --category pokemon-cards
dotnet run --project src/P2W.DealFinder.Worker -- pokemon-catalog-build --category pokemon-cards --output data/generated/pokemon_master_catalog.csv
dotnet run --project src/P2W.DealFinder.Worker -- pokemon-catalog-import --csv data/generated/pokemon_master_catalog.csv --dry-run --dry-run
dotnet run --project src/P2W.DealFinder.Worker -- pricecharting-import --category pokemon-cards
dotnet run --project src/P2W.DealFinder.Worker -- pokemon-catalog-build --category pokemon-cards --output data/generated/pokemon_master_catalog.csv
dotnet run --project src/P2W.DealFinder.Worker -- pokemon-catalog-import --csv data/generated/pokemon_master_catalog.csv --dry-run
```

Useful switches:

- `--limit 500`: test a bounded write before the full import.
- `--include-non-english`: keep rows that the simple language heuristic would normally skip.
- `--include-missing-psa10`: keep products without a PSA 10 value.
- `--target-connection "..."`: override the default `P2WDealFinderDb` target.

Inspect the result in SSMS with `docs/sql/queries/06_pricecharting_snapshot_exploration.sql`.
## Pokemon Master Catalog CSV

The local catalog spine can now be generated as a CSV before importing into SQL:

```powershell
dotnet run --project src/P2W.DealFinder.Worker -- pokemon-catalog-build --category pokemon-cards --output data/generated/pokemon_master_catalog.csv
dotnet run --project src/P2W.DealFinder.Worker -- pokemon-catalog-import --csv data/generated/pokemon_master_catalog.csv --dry-run
dotnet run --project src/P2W.DealFinder.Worker -- pokemon-catalog-import --csv data/generated/pokemon_master_catalog.csv
```

The first full English build produced 44,987 catalog rows from 88,474 PriceCharting provider rows. Generated CSV files live under `data/generated/` and are ignored by git. See `docs/catalog/POKEMON_MASTER_CATALOG_CSV.md` and `docs/sql/queries/07_pokemon_master_catalog_exploration.sql`.

