# SQL Notes

This folder is for read-only SQL first. The goal is to make imported products, market evidence, provider coverage, and deal candidates understandable from SSMS before adding more automation.

Start with `queries/04_deal_finder_exploration.sql` after the new schema exists.

## Query Files

- queries/01_catalog_bridge_validation.sql: validates the first imported catalog slice, starting with Pokemon Chaos Rising in P2WDealFinderDb.
- queries/04_deal_finder_exploration.sql: planned deal/evidence exploration queries for later phases.
- queries/06_pricecharting_snapshot_exploration.sql: explores PriceCharting product/snapshot imports, PSA 10 price bands, volume, and snapshot deltas.
- queries/07_pokemon_master_catalog_exploration.sql: explores the CSV-backed Pokemon master catalog, release dates, provider links, PSA 10 ranges, volume, and classifier review rows.

