# Product Data Completeness Plan

Living plan for making product detail data reliable before adding more marketplace features.

Current date: 2026-06-15.

## Why This Comes First

The marketplace and deal-scout features only become useful when each product has a trustworthy identity, complete product metadata, and a clear market-evidence status.

Right now, the catalog is good enough to browse sets and refresh market rows, but product details are still shallow. Some missing fields are true gaps in our database shape, and some market rows looked missing because the set dashboard did not return every product row to the frontend.

## Validation Snapshot

Sample checked: `Dawn`, Pokemon, `Phantasmal Flames`, card `#118`.

Validated against the local API and the external PokemonTCG record:

- Catalog identity is correct: provider mapping resolves to `me2-118`.
- Set data is correct: `Phantasmal Flames`, `ME2`, release date `2025-11-14`.
- Core card fields are correct: name `Dawn`, number `118`, rarity `Ultra Rare`, artist `Yuu Nishida`.
- Image URL is correct and points to the PokemonTCG high-resolution asset.
- PokemonTCG reference price is present for `holofoil` with market around `$6.60`.
- eBay active listings are present, but no sold comps were captured for the sampled refresh.

Important caveats:

- The current product detail page does not persist or display richer Pokemon fields like supertype, subtypes, rules text, legalities, attacks, weaknesses, retreat cost, HP, or card types.
- Existing imported variants can be too naive for Pokemon. Dawn `#118` has PokemonTCG pricing for `holofoil`, but current database rows can still show older inferred normal/reverse variants until we reimport or backfill them.
- Market confidence must be split from identity confidence. A perfect product match is not the same as a trustworthy market price.
- Active eBay listings help with availability and opportunity discovery, but sold comps are required before we can call a price highly reliable.
- Outliers like very high listing prices must be filtered or clearly separated from normal market range.

## Immediate Fixes

- [x] Move set card listings below the analytics sections so rankings and set insights are visible first.
- [x] Return all set dashboard product rows from the API instead of only top-ranked subsets.
- [x] Use the full dashboard product list on the frontend before falling back to merged top-ranked rows.
- [x] Populate future Pokemon imports with rules text in `CatalogProduct.Description` when provider rules are present.
- [x] Build future Pokemon variants from TCGplayer price keys before falling back to generic inferred variants.
- [ ] Restart the API/frontend and confirm the set catalog no longer shows false `Pending` rows for products that already have market rows.
- [ ] Reimport or backfill existing Pokemon rows so descriptions and corrected variants are written to the database.
- [ ] Add a visible freshness/coverage row to each set screen: products in set, products refreshed, products with reference price, products with active listings, products with sold comps.

## Product Detail Field Targets

### Common Fields

These should be available for every catalog product where the provider can supply them.

- [ ] Source provider and external id.
- [ ] Last metadata refresh time.
- [ ] Source data version/hash so unchanged provider payloads do not cause noisy writes.
- [ ] Product type and category.
- [ ] Set name, set code, release date, set symbol/logo where available.
- [ ] Product image status: present, missing, failed, stale.
- [ ] Variant/finish list derived from provider data, not generic guesses.
- [ ] Completeness score and missing-field reasons.

### Pokemon Singles

- [ ] Supertype.
- [ ] Subtypes.
- [ ] HP.
- [ ] Energy/types.
- [ ] Evolves from / evolves to.
- [ ] Rules text.
- [ ] Attacks: name, cost, damage, text.
- [ ] Abilities where present.
- [ ] Weaknesses, resistances, retreat cost.
- [ ] Legalities.
- [ ] Regulation mark.
- [ ] TCGplayer URL and price variant keys when present.
- [ ] Finish support: normal, holofoil, reverse holofoil, first edition, promo where applicable.

### One Piece Singles

- [ ] Card type.
- [ ] Color.
- [ ] Cost.
- [ ] Power.
- [ ] Counter.
- [ ] Attribute.
- [ ] Effect text.
- [ ] Trigger text.
- [ ] Life.
- [ ] Rarity.
- [ ] Set and parallel/art treatment.

### Magic Singles

- [ ] Mana cost and mana value.
- [ ] Type line.
- [ ] Oracle/rules text.
- [ ] Power/toughness or loyalty.
- [ ] Colors and color identity.
- [ ] Legalities.
- [ ] Finishes.
- [ ] All-printings relationship.

## Data Model Direction

Use a hybrid model:

- Keep common product fields on `CatalogProduct`.
- Add a game-specific metadata table or owned JSON payload for fields that differ heavily by game.
- Keep raw provider payload snapshots for audit, reprocessing, and future backfills.
- Add a provider coverage table so we can distinguish: identity matched, metadata complete, reference price found, active listings found, sold comps found, and no-data reason.

Recommended new concepts:

- `CatalogProductMetadata`: one row per product with common display metadata and completeness score.
- `CatalogProductGameMetadata`: game-specific JSON plus normalized display fields.
- `CatalogProductProviderSnapshot`: raw provider payload, provider id, fetched time, hash, and status.
- `CatalogProductProviderCoverage`: latest status per product/provider/source type.

## Backfill Strategy

1. Parse existing `RawSourceJson` first so we can fill many fields without calling APIs.
2. Extend provider DTOs and normalizers to expose rich fields.
3. Update import/upsert code to write product descriptions and typed metadata.
4. Add a backfill command/job that processes one game or one set at a time.
5. Add a dry-run mode that reports changed fields before writing.
6. Add a missing-data report grouped by game, set, provider, and field.
7. Use provider API calls only for records without raw JSON, stale snapshots, or known bad mappings.

## Validation Rules

- A set is catalog-complete only when local product count matches the provider set total.
- A product is identity-verified only when provider id, name, set, and card number agree.
- A product is detail-complete only when required fields for its game/product type are present.
- A market row is reference-backed when it has a provider reference price.
- A market row is trade-backed when it has sold comps.
- A deal signal is actionable only when expected value, fees, shipping, net profit, ROI, match confidence, and liquidity are all present.

## Next Build Order

1. Implement the metadata field model and Pokemon rich-field import/backfill.
2. Backfill the current Pokemon catalog by set so corrected descriptions and variants are stored.
3. Add set/product completeness reporting to the UI.
4. Add provider coverage/no-data reason reporting to the UI.
5. Improve market confidence so no sold comps lowers confidence even when active listings exist.
6. Add outlier filtering for listing highs and reference highs.
7. Add richer game-specific metadata storage.
8. Repeat the same metadata pattern for One Piece.
