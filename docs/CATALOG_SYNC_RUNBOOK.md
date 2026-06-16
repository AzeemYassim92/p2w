# Catalog Sync Runbook

Current date: 2026-06-15.

This runbook covers terminal-only catalog sync and validation jobs. These jobs let us populate and verify base set/card catalogs without launching the frontend.

## Commands

Pokemon full set/card sync:

```powershell
dotnet run --project src/P2W.Cards.Worker.Aggregation -- sync-pokemon-catalog --batch-size 5000
```

One Piece full set/card sync:

```powershell
dotnet run --project src/P2W.Cards.Worker.Aggregation -- sync-onepiece-catalog --batch-size 5000
```

Validate Pokemon without importing:

```powershell
dotnet run --project src/P2W.Cards.Worker.Aggregation -- validate-catalog pokemon
```

Validate One Piece without importing:

```powershell
dotnet run --project src/P2W.Cards.Worker.Aggregation -- validate-catalog one-piece
```

Resume a failed or interrupted sync from the saved checkpoint:

```powershell
dotnet run --project src/P2W.Cards.Worker.Aggregation -- catalog-sync pokemon --resume --batch-size 5000
dotnet run --project src/P2W.Cards.Worker.Aggregation -- catalog-sync one-piece --resume --batch-size 5000
```

## What Gets Written

The sync jobs write:

- `CardSets`
- `CatalogProducts`
- product images and basic metadata when the source provides them
- external provider mappings
- product variants where the source has variant/finish clues
- import run history, errors, and checkpoints

They do not write marketplace listings, sold comps, price history, inventory, carts, orders, or user data.

## Provider Sources

Pokemon uses the PokemonTCG API:

- sets come from `/sets`
- cards come from `/cards`
- validation compares local counts against provider `totalCount`
- set spot-checks compare local product counts against provider set totals

One Piece uses the official Bandai card list:

- set ids come from the card list selector
- each selected set page is parsed for card rows
- validation compares local set/card counts against the official card list
- alternate-art rows are counted as official rows but collapsed into one canonical marketplace product per set, card number, and name

## Completion Criteria

A catalog sync is healthy when:

- provider set count and local set count have no unexplained delta
- provider/official card count and local active single-card product count have no unexplained delta
- recent set spot-checks show `OK`
- products for a selected marketplace set load with at least name, set, card number, rarity, and image where available
- `logs/session.log` shows provider calls and import batches without repeated provider failures

## Expected Deltas

Some deltas can be legitimate:

- Pokemon provider totals include variants or special product records differently than our current canonical product model.
- One Piece official pages can include parallel/alternate art rows. We collapse those into canonical products and preserve the alternate external ids through mappings/variants.
- Promotional or other product card sections may not behave exactly like normal booster sets.
- Seeded sealed products are intentionally excluded from card-count validation.
- If a spot-check shows `local by name`, the products exist but the local set code is stale. Rerun the relevant sync job so the importer can repair the set identity from the provider.

When a delta appears, inspect the set spot-check output first. A few specific set mismatches are easier to diagnose than a global product count mismatch.
