# JustTCG Provider Notes

Last updated: 2026-06-16

## Role In DealFinder

JustTCG is the first reference-pricing provider for the DealFinder MVP.

Use it for:
- Card identity resolution.
- Current raw-card variant pricing.
- Condition and printing-specific price signals.
- Historical price points for charts and trend analysis.
- Catalog sanity checks against our imported product data.

Do not use it as the only confidence source for buying decisions until we confirm whether it exposes sold volume, last sold, or liquidity fields. Active/reference pricing is useful, but sold comps still drive trustworthy deal confidence.

## Confirmed Public Endpoints

Base URL:

```text
https://api.justtcg.com/v1
```

Endpoints seen in current docs:

```text
GET  /games
GET  /sets
GET  /cards
POST /cards
```

The current public docs model variants as part of the card response. There is not a standalone public `/variants` endpoint in the docs reviewed today.

## Implemented Worker Commands

```powershell
dotnet run --no-build --project .\src\P2W.DealFinder.Worker\P2W.DealFinder.Worker.csproj -- justtcg-games
dotnet run --no-build --project .\src\P2W.DealFinder.Worker\P2W.DealFinder.Worker.csproj -- justtcg-sets --game pokemon
dotnet run --no-build --project .\src\P2W.DealFinder.Worker\P2W.DealFinder.Worker.csproj -- justtcg-cards --game pokemon --name Charizard --price-history 180d --limit 3
dotnet run --no-build --project .\src\P2W.DealFinder.Worker\P2W.DealFinder.Worker.csproj -- justtcg-range --game pokemon --set "Chaos Rising" --min 15 --max 25 --price-history 180d --take 40
```

The commands read the API key from:
- `--justtcg-key`
- `JUSTTCG_API_KEY`
- `DEALFINDER_JUSTTCG_API_KEY`
- old `ecompt2` `Providers:JustTcg:ApiKey`

The key is never printed by the worker.

## Card Query Support

The new client supports these `/cards` filters:

- `cardId`
- `variantId`
- `game`
- `set`
- `q`
- `name`
- `number`
- `tcgplayerId`
- `tcgplayerSkuId`
- `mtgjsonId`
- `scryfallId`
- `condition`
- `printing`
- `min_price`
- `include_null_prices`
- `updated_after`
- `orderBy`
- `order`
- `includePriceHistory`
- `includeStatistics`
- `priceHistoryDuration`
- `limit`
- `offset`

## Important Response Fields

Card-level fields mapped:

- `id`
- `uuid`
- `name`
- `game`
- `set`
- `set_name`
- `number`
- `rarity`
- `tcgplayerId`
- `details`
- `variants`

Variant-level fields mapped:

- `id`
- `uuid`
- `condition`
- `printing`
- `language`
- `price`
- `tcgplayerSkuId`
- `lastUpdated`
- `priceChange24hr`
- `priceChange7d`
- `priceChange30d`
- `priceChange90d`
- `avgPrice`
- `avgPrice30d`
- `avgPrice90d`
- `priceHistory`

Price history point fields mapped:

- `p`
- `t`
- `price`
- `date`
- `timestamp`

Unknown JSON fields are preserved through `JsonExtensionData`, which lets us detect future/hidden fields such as `psa9`, `psa10`, `graded`, or `ungraded` without rewriting the DTOs first.

## Graded Value Status

Not validated yet.

The DTO/probe code scans variant extension fields for names containing:

- `psa`
- `grade`
- `graded`
- `ungraded`

The live API smoke test could not inspect card payloads because the stored key currently returns:

```text
401 Unauthorized: INVALID_API_KEY
```

Once a valid key is available, run:

```powershell
dotnet run --no-build --project .\src\P2W.DealFinder.Worker\P2W.DealFinder.Worker.csproj -- justtcg-cards --game pokemon --name Charizard --price-history 180d --limit 3
```

or:

```powershell
dotnet run --no-build --project .\src\P2W.DealFinder.Worker\P2W.DealFinder.Worker.csproj -- justtcg-cards --game pokemon --name Charizard --price-history 180d --limit 1 --raw
```

Then inspect whether graded values appear directly in variants, nested details, or only through separate product variants.

## Current Blocker

The old `ecompt2` local config contains a JustTCG key with the expected `tcg_` prefix and 36 characters, but JustTCG rejects it. Tested header shapes:

- `x-api-key`
- `X-API-Key`
- `Authorization: Bearer <key>`
- raw `Authorization`
- `api-key`

All returned `401`.

Next action: generate or copy a fresh JustTCG key into either `JUSTTCG_API_KEY` or the old local appsettings, then rerun the smoke commands.

## Next Data Model Step

After a successful live card response, add persistence tables for:

- `ReferencePriceSnapshots`
- `ReferencePriceVariants`
- `ReferencePriceHistoryPoints`
- `ProviderFetchLogs`

Keep the product catalog separate from pricing evidence. Pricing evidence can have many rows per product and should be refreshed independently.
