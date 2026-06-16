# P2W SQL Exploration Runbook

Current date: 2026-06-15.

This folder is for understanding the database from SSMS before we keep scaling imports, market aggregation, and deal logic.

The goal is not to hide complexity behind code. The goal is to make the data explain itself.

## Folder Map

| File | Purpose |
| --- | --- |
| `queries/00_schema_relationships.sql` | Table sizes, foreign keys, indexes, and nullable column inspection. |
| `queries/01_catalog_exploration.sql` | Human-readable catalog counts, sets, products, variants, mappings, duplicates, and missing metadata. |
| `queries/02_import_quality.sql` | Import runs, checkpoints, errors, source mappings, stale seed codes, and catalog bottlenecks. |
| `queries/03_market_aggregation_exploration.sql` | Active listings, sold comps, snapshots, metrics, confidence, provider coverage, and deal-signal sanity checks. |
| `views/01_catalog_market_views.sql` | Optional `CREATE OR ALTER VIEW` helpers for repeated SSMS browsing. |
| `stored-procedures/01_diagnostic_procedures.sql` | Optional parameterized audit procedures for set/product/import inspection. |

## Suggested SSMS Order

1. Open `queries/00_schema_relationships.sql`.
2. Run section 1 to see row counts by table.
3. Run section 2 to see foreign-key relationships.
4. Run section 3 to see high-value indexes.
5. Open `queries/01_catalog_exploration.sql`.
6. Run the catalog overview, set coverage, and duplicate checks.
7. Open `queries/02_import_quality.sql`.
8. Check the latest import runs, import errors, checkpoints, and stale set-code candidates.
9. Open `queries/03_market_aggregation_exploration.sql`.
10. Check market source coverage, listings vs sold comps, snapshots, and confidence bottlenecks.

Only after those read-only checks should you optionally run:

```sql
-- Optional convenience objects
-- These create views/procs in dbo.
\i docs/sql/views/01_catalog_market_views.sql
\i docs/sql/stored-procedures/01_diagnostic_procedures.sql
```

SSMS does not support `\i`; that line is just a reminder. In SSMS, open each file and execute it manually.

## What To Look For First

### Catalog Foundation

The catalog is healthy when:

- `CatalogProducts` single-card counts line up with provider card counts.
- `CardSets.Code` uses provider codes, not old placeholder seed codes.
- Products have set, card number, rarity, image, and external mapping coverage.
- Duplicate groups are explainable: true reprints are fine, accidental duplicate same-set same-number same-name rows are not.

### Import Health

Imports are healthy when:

- Recent `CatalogImportRuns` finish as `Completed`.
- `CatalogImportErrors` are rare and explainable.
- Checkpoints do not get stuck on the same value.
- `ExternalProductMappings` cover most imported single-card products.

### Market Health

Market data is healthy when:

- Reference prices, active listings, and sold comps are tracked separately.
- Confidence is not high when sold comps are missing.
- Excluded listings have clear exclusion reasons.
- Deal signals include realistic fees, shipping, net margin, ROI, and confidence.

## Important Table Families

Catalog identity:

- `Games`
- `CardSets`
- `ProductCategories`
- `CatalogProducts`
- `ProductVariants`
- `ExternalProductMappings`

Catalog import:

- `CatalogImportRuns`
- `CatalogImportErrors`
- `CatalogImportCheckpoints`

Reference prices:

- `CatalogPriceReferenceSnapshots`

Market aggregation:

- `MarketplaceSources`
- `ExternalMarketplaceSkuMappings`
- `CatalogMarketplaceListings`
- `CatalogMarketplaceSales`
- `CatalogMarketPriceSnapshots`
- `CatalogMarketMetrics`
- `CatalogProviderIngestionRuns`
- `CatalogProviderIngestionErrors`
- `CatalogAggregationCheckpoints`

User/business surfaces:

- `SellerInventoryItems`
- `SellerInventoryImages`
- `CatalogWatchlistItems`
- `ProductMarketViewEvents`

Legacy MVP tables:

- `Cards`
- `CardVariants`
- `Listings`
- `PriceSnapshots`
- `PriceReferenceSnapshots`
- `WatchlistItems`
- `PriceAlerts`

The newer product catalog and market aggregation work is built around `CatalogProducts`, not the older `Cards` table.

## Safe Usage Notes

- Query files are read-only.
- View and stored-procedure files create or replace helper objects only. They do not update product or market data.
- Do not run ad hoc `UPDATE` or `DELETE` statements while exploring. If a query reveals bad data, fix it through importer/backfill code first unless we intentionally write a migration or repair script.
