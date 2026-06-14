# P2W Cards Marketplace Aggregation Context

This document captures the current P2W Cards data model and API surface so the marketplace aggregation system can be designed with enough local context. It is intentionally a design handoff document, not just a schema dump.

Current date of this snapshot: 2026-06-14.

## Current Architecture

- Backend: ASP.NET Core / .NET 9.
- Database access: Entity Framework Core.
- Current database context: `CardsDbContext`.
- Frontend: React + Vite.
- Current product catalog direction: the newer canonical catalog model centered on `CatalogProduct`.
- Older MVP card model still exists: `Card`, `CardVariant`, `Listing`, `PriceSnapshot`, `PriceReferenceSnapshot`.

For marketplace aggregation, prefer the newer catalog product model:

- `CatalogProduct`
- `ProductVariant`
- `ExternalProductMapping`
- `CatalogPriceReferenceSnapshot`
- future catalog-level listing/sales/market metric tables

The older `Card` tables are still useful as historical/MVP reference, but they should not be the foundation for the next marketplace aggregation design unless we intentionally migrate them into the catalog model.

## Current Entity Groups

### Canonical Catalog

These tables define the product catalog shown on the marketplace home page and product detail page.

#### `Games`

Entity: `Game`

Purpose: supported trading card games and collectible categories.

Fields:

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | `Guid` | Primary key |
| `Name` | `string` | Max length 120 |
| `Slug` | `string` | Max length 140, unique |
| `Description` | `string?` | Optional display copy |
| `IsPrimaryFocus` | `bool` | Used for current marketplace tabs |
| `IsActive` | `bool` | Visibility/availability flag |
| `DisplayOrder` | `int` | UI ordering |
| `CreatedUtc` | `DateTime` | Creation timestamp |
| `UpdatedUtc` | `DateTime` | Update timestamp |

Indexes:

- Unique: `Slug`
- Non-unique: `Name`

Seeded primary focus games:

- Magic: The Gathering
- Pokemon
- One Piece

Seeded future games:

- Yu-Gi-Oh!
- Disney Lorcana
- Digimon
- Star Wars: Unlimited
- Flesh and Blood

#### `CardSets`

Entity: `CardSet`

Purpose: game-specific sets/releases.

Fields:

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | `Guid` | Primary key |
| `GameId` | `Guid` | FK to `Games.Id` |
| `Name` | `string` | Max length 180 |
| `NormalizedName` | `string` | Max length 180, used for matching |
| `Slug` | `string` | Max length 200 |
| `Code` | `string?` | Set code, examples: `FIN`, `DRI`, `OP01` |
| `ReleaseDate` | `DateTime?` | Optional release date |
| `IsUpcoming` | `bool` | Marks future/preorder sets |
| `IsActive` | `bool` | Visibility flag |
| `LogoUrl` | `string?` | Optional set logo |
| `SymbolUrl` | `string?` | Optional set symbol |
| `CreatedUtc` | `DateTime` | Creation timestamp |
| `UpdatedUtc` | `DateTime` | Update timestamp |

Indexes:

- `GameId`
- `Slug`
- Unique: `GameId`, `Name`
- `GameId`, `NormalizedName`
- `GameId`, `Code`

Aggregation relevance:

- Price comparisons should almost always be scoped to product + set + variant, not only product name.
- Upcoming/preorder sets will need special handling because price availability and volatility behave differently before release.

#### `ProductCategories`

Entity: `ProductCategory`

Purpose: marketplace browsing categories.

Fields:

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | `Guid` | Primary key |
| `ParentCategoryId` | `Guid?` | Self-referencing parent category |
| `Name` | `string` | Max length 120 |
| `Slug` | `string` | Max length 140, unique |
| `Description` | `string?` | Optional display copy |
| `IsActive` | `bool` | Visibility flag |
| `DisplayOrder` | `int` | UI ordering |
| `CreatedUtc` | `DateTime` | Creation timestamp |
| `UpdatedUtc` | `DateTime` | Update timestamp |

Indexes:

- Unique: `Slug`
- `ParentCategoryId`

Seeded categories include:

- Singles
- Raw Singles
- Graded Cards
- Foils
- Reverse Holos
- Promos
- Sealed
- Booster Packs
- Booster Boxes
- Elite Trainer Boxes
- Bundles
- Starter Decks
- Commander Decks
- Collection Boxes
- Accessories
- Supplies
- Bulk Lots
- Complete Sets
- Preorders
- Deals

Aggregation relevance:

- Different categories need different marketplace matching rules.
- Singles should match by game, set, number, name, treatment, language, condition.
- Sealed products should match by sealed type, set, UPC/GTIN when available, box/pack/case quantity.
- Graded cards need grading company, grade, cert number, population data if available.

#### `CatalogProducts`

Entity: `CatalogProduct`

Purpose: canonical product record for the newer marketplace catalog.

Fields:

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | `Guid` | Primary key |
| `GameId` | `Guid` | FK to `Games.Id` |
| `CardSetId` | `Guid?` | FK to `CardSets.Id`; optional |
| `ProductCategoryId` | `Guid` | FK to `ProductCategories.Id` |
| `Name` | `string` | Max length 240 |
| `NormalizedName` | `string` | Max length 240 |
| `Slug` | `string` | Max length 260 |
| `ProductType` | `string` | Max length 80; examples: `SingleCard`, `BoosterPack`, `BoosterBox`, `EliteTrainerBox` |
| `CardNumber` | `string?` | Collector/card number |
| `Rarity` | `string?` | Product rarity where applicable |
| `Artist` | `string?` | Card artist where applicable |
| `Description` | `string?` | Rules text/product copy |
| `ImageUrl` | `string?` | Product image |
| `ReleaseDate` | `DateTime?` | Product release date |
| `IsSealed` | `bool` | Sealed product flag |
| `IsSingleCard` | `bool` | Single card flag |
| `IsGradedEligible` | `bool` | Whether graded variants make sense |
| `IsActive` | `bool` | Visibility flag |
| `IsFeatured` | `bool` | Current manual featured flag |
| `IsTrending` | `bool` | Current manual trending flag |
| `CreatedUtc` | `DateTime` | Creation timestamp |
| `UpdatedUtc` | `DateTime` | Update timestamp |

Indexes:

- `GameId`
- `CardSetId`
- `ProductCategoryId`
- `Slug`
- `Name`
- `NormalizedName`
- `ProductType`
- `IsFeatured`
- `IsTrending`
- `GameId`, `Name`, `CardSetId`
- `GameId`, `CardSetId`, `CardNumber`, `NormalizedName`

Aggregation relevance:

- This is the table market aggregation should attach to.
- `IsFeatured` and `IsTrending` are currently flags, but should eventually be driven by computed market signals.
- The existing duplicate-looking products are often print variants or set variants. Aggregation must be precise enough to avoid merging distinct printings incorrectly.

#### `ProductVariants`

Entity: `ProductVariant`

Purpose: variant dimensions for catalog products.

Fields:

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | `Guid` | Primary key |
| `CatalogProductId` | `Guid` | FK to `CatalogProducts.Id` |
| `VariantName` | `string` | Example: `Normal` |
| `Language` | `string?` | Example: `English` |
| `IsFoil` | `bool` | Foil flag |
| `IsReverseHolo` | `bool` | Reverse holo flag |
| `IsFirstEdition` | `bool` | First edition flag |
| `IsPromo` | `bool` | Promo flag |
| `IsSerialized` | `bool` | Serialized card flag |
| `IsSealedCase` | `bool` | Sealed case flag |
| `CreatedUtc` | `DateTime` | Creation timestamp |

Indexes:

- `CatalogProductId`
- `CatalogProductId`, `VariantName`

Aggregation relevance:

- Market prices should usually be variant-aware.
- More fields may be needed for full aggregation: condition, printing/treatment, finish, language, first edition, stamped/promotional, serialized number, sealed unit size, region, grading metadata.

### Catalog Import and Mapping

These tables support importing product metadata from external catalog providers.

#### `ExternalProductMappings`

Entity: `ExternalProductMapping`

Purpose: maps external provider records to canonical `CatalogProduct` records.

Fields:

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | `Guid` | Primary key |
| `CatalogProductId` | `Guid` | FK to `CatalogProducts.Id` |
| `SourceName` | `string` | Provider/source name, examples: `Scryfall`, `PokemonTCG` |
| `ExternalId` | `string` | External provider product id |
| `ExternalUrl` | `string?` | External product URL |
| `ExternalSlug` | `string?` | External slug |
| `ConfidenceScore` | `decimal?` | Precision 18,2 |
| `MappingStatus` | `string` | Max length 40, default `AutoMatched` |
| `MappingNotes` | `string?` | Human review notes |
| `CreatedUtc` | `DateTime` | Creation timestamp |
| `LastVerifiedUtc` | `DateTime?` | Last mapping verification timestamp |

Indexes:

- `CatalogProductId`
- Unique: `SourceName`, `ExternalId`
- `MappingStatus`

Current review statuses seen in code:

- `AutoMatched`
- approved/rejected flows exist through mapping review service

Aggregation relevance:

- This is the critical bridge for price/listing providers.
- It may need expansion to map external IDs at `ProductVariant` level, not only `CatalogProduct` level.
- A mature system likely needs a separate mapping table for marketplace SKUs/offers because catalog IDs and sales/listing IDs are often different.

#### `CatalogImportRuns`

Entity: `CatalogImportRun`

Purpose: records catalog import jobs.

Fields:

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | `Guid` | Primary key |
| `SourceName` | `string` | Import provider |
| `ImportType` | `string` | Example: `Cards` |
| `StartedUtc` | `DateTime` | Start timestamp |
| `FinishedUtc` | `DateTime?` | Finish timestamp |
| `RecordsProcessed` | `int` | Count |
| `RecordsCreated` | `int` | Count |
| `RecordsUpdated` | `int` | Count |
| `RecordsSkipped` | `int` | Count |
| `ErrorCount` | `int` | Count |
| `Status` | `string` | Example: `Started`, `Completed`, `Failed` |
| `Notes` | `string?` | Summary/checkpoint info/errors |

Indexes:

- `SourceName`
- `ImportType`
- `StartedUtc`
- `Status`

#### `CatalogImportErrors`

Entity: `CatalogImportError`

Purpose: per-record import errors.

Fields:

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | `Guid` | Primary key |
| `CatalogImportRunId` | `Guid` | FK to `CatalogImportRuns.Id` |
| `SourceName` | `string` | Provider |
| `ExternalId` | `string?` | External record id |
| `ErrorMessage` | `string` | Error text |
| `RawSourceJson` | `string?` | Raw provider payload |
| `CreatedUtc` | `DateTime` | Error timestamp |

Indexes:

- `CatalogImportRunId`
- `SourceName`
- `ExternalId`

#### `CatalogImportCheckpoints`

Entity: `CatalogImportCheckpoint`

Purpose: stores resumable import checkpoints.

Fields:

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | `Guid` | Primary key |
| `SourceName` | `string` | Max length 100 |
| `ImportType` | `string` | Max length 40 |
| `CheckpointValue` | `string` | Provider-specific checkpoint |
| `UpdatedUtc` | `DateTime` | Last checkpoint update |

Indexes:

- Unique: `SourceName`, `ImportType`

Aggregation relevance:

- Similar checkpointing will be needed for listing/price/sales ingestion.
- Aggregation checkpoints should likely separate import type by provider and workload: `Listings`, `Sales`, `PriceReferences`, `SkuCatalog`, `Buylist`, etc.

### Catalog Pricing

#### `CatalogPriceReferenceSnapshots`

Entity: `CatalogPriceReferenceSnapshot`

Purpose: stores source-provided price references for catalog products.

Fields:

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | `Guid` | Primary key |
| `CatalogProductId` | `Guid` | FK to `CatalogProducts.Id` |
| `ProductVariantId` | `Guid?` | Optional FK to `ProductVariants.Id` |
| `SourceName` | `string` | Price source |
| `MarketPrice` | `decimal?` | Precision 18,2 |
| `LowPrice` | `decimal?` | Precision 18,2 |
| `MidPrice` | `decimal?` | Precision 18,2 |
| `HighPrice` | `decimal?` | Precision 18,2 |
| `UngradedPrice` | `decimal?` | Precision 18,2 |
| `Grade7Price` | `decimal?` | Precision 18,2 |
| `Grade8Price` | `decimal?` | Precision 18,2 |
| `Grade9Price` | `decimal?` | Precision 18,2 |
| `Grade10Price` | `decimal?` | Precision 18,2 |
| `BuylistPrice` | `decimal?` | Precision 18,2 |
| `RetailPrice` | `decimal?` | Precision 18,2 |
| `Currency` | `string` | Default `USD` |
| `RawSourceJson` | `string?` | Raw provider payload |
| `CapturedAtUtc` | `DateTime` | Snapshot timestamp |

Indexes:

- `CatalogProductId`
- `ProductVariantId`
- `SourceName`
- `CapturedAtUtc`
- `CatalogProductId`, `SourceName`, `CapturedAtUtc`

Aggregation relevance:

- This is the current closest table to the future price history model.
- It handles reference prices, but not current live listings, sold comps, velocity, rank, or margin analytics.
- It does not yet represent condition-specific raw prices beyond coarse graded price columns.

### Seller Inventory

These tables represent inventory listed by sellers inside P2W.

#### `SellerInventoryItems`

Entity: `SellerInventoryItem`

Purpose: seller-owned inventory for the marketplace.

Fields:

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | `Guid` | Primary key |
| `SellerUserId` | `Guid` | Current placeholder user id source |
| `CatalogProductId` | `Guid` | FK to `CatalogProducts.Id` |
| `ProductVariantId` | `Guid?` | Optional FK to `ProductVariants.Id` |
| `Condition` | `ProductCondition` | Enum |
| `RawConditionNotes` | `string?` | Seller notes |
| `IsGraded` | `bool` | Graded flag |
| `GradingCompany` | `string?` | Example: PSA, CGC, BGS |
| `Grade` | `decimal?` | Precision 18,2 |
| `CertificationNumber` | `string?` | Grading cert |
| `Quantity` | `int` | Quantity available |
| `AskingPrice` | `decimal?` | Precision 18,2 |
| `Currency` | `string` | Default `USD` |
| `IsAvailableForSale` | `bool` | Availability flag |
| `CreatedUtc` | `DateTime` | Creation timestamp |
| `UpdatedUtc` | `DateTime` | Update timestamp |

Indexes:

- `SellerUserId`
- `CatalogProductId`
- `ProductVariantId`
- `Condition`
- `IsAvailableForSale`

`ProductCondition` enum:

| Value | Name |
| --- | --- |
| `0` | `Unknown` |
| `10` | `NearMint` |
| `20` | `LightlyPlayed` |
| `30` | `ModeratelyPlayed` |
| `40` | `HeavilyPlayed` |
| `50` | `Damaged` |
| `60` | `Sealed` |
| `70` | `Opened` |
| `80` | `Graded` |

Aggregation relevance:

- Internal inventory should eventually participate in market comparisons.
- Margin/opportunity calculations need seller cost basis, acquisition date, fees, shipping, and fulfillment costs. Those fields do not exist yet.

#### `SellerInventoryImages`

Entity: `SellerInventoryImage`

Purpose: images attached to seller inventory.

Fields:

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | `Guid` | Primary key |
| `SellerInventoryItemId` | `Guid` | FK to `SellerInventoryItems.Id` |
| `ImageUrl` | `string` | Image URL |
| `DisplayOrder` | `int` | Image ordering |
| `CreatedUtc` | `DateTime` | Creation timestamp |

Indexes:

- `SellerInventoryItemId`

## Legacy/MVP Card and Pricing Model

These tables exist and power parts of the older MVP APIs. They overlap with the new catalog model.

### `Cards`

Entity: `Card`

Purpose: original canonical card table.

Fields:

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | `Guid` | Primary key |
| `Name` | `string` | Max length 200 |
| `Game` | `string` | Max length 40 |
| `SetName` | `string` | Max length 200 |
| `SetCode` | `string?` | Optional |
| `CardNumber` | `string?` | Optional |
| `Rarity` | `string?` | Optional |
| `Artist` | `string?` | Optional |
| `ImageUrl` | `string?` | Optional |
| `CreatedUtc` | `DateTime` | Creation timestamp |
| `UpdatedUtc` | `DateTime` | Update timestamp |

Indexes:

- `Name`
- `Game`
- `SetName`
- `Game`, `Name`, `SetName`

### `CardVariants`

Entity: `CardVariant`

Purpose: original variant model for `Card`.

Fields:

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | `Guid` | Primary key |
| `CardId` | `Guid` | FK to `Cards.Id` |
| `VariantName` | `string` | Variant label |
| `Language` | `string?` | Optional |
| `IsFoil` | `bool` | Foil flag |
| `IsReverseHolo` | `bool` | Reverse holo flag |
| `IsFirstEdition` | `bool` | First edition flag |
| `IsGraded` | `bool` | Graded flag |
| `GradingCompany` | `string?` | Optional |
| `Grade` | `decimal?` | Precision 18,2 |
| `CreatedUtc` | `DateTime` | Creation timestamp |

Indexes:

- `CardId`

### `ExternalSources`

Entity: `ExternalSource`

Purpose: source/provider registry.

Fields:

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | `Guid` | Primary key |
| `Name` | `string` | Max length 100, unique |
| `ProviderType` | `string` | Examples: `Catalog`, `MarketplaceListing`, `PriceReference` |
| `IsActive` | `bool` | Whether source is active |
| `PriorityRank` | `int` | Ordering/priority |
| `CreatedUtc` | `DateTime` | Creation timestamp |

Seeded sources:

- Mock
- TCGplayer
- eBay
- Scryfall
- MTGJSON
- PokemonTCG
- PriceCharting
- CardKingdom
- Cardmarket

### `ExternalCardMappings`

Entity: `ExternalCardMapping`

Purpose: original mapping between `Cards` and external IDs.

Fields:

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | `Guid` | Primary key |
| `CardId` | `Guid` | FK to `Cards.Id` |
| `SourceName` | `string` | External source |
| `ExternalId` | `string` | External id |
| `ExternalUrl` | `string?` | External URL |
| `ExternalSlug` | `string?` | External slug |
| `CreatedUtc` | `DateTime` | Creation timestamp |
| `LastVerifiedUtc` | `DateTime?` | Last verification |

Indexes:

- `CardId`
- Unique: `SourceName`, `ExternalId`

### `Marketplaces`

Entity: `Marketplace`

Purpose: original marketplace registry for listings.

Fields:

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | `Guid` | Primary key |
| `Name` | `string` | Max length 100, unique |
| `BaseUrl` | `string` | Marketplace base URL |
| `IsActive` | `bool` | Whether marketplace is active |
| `CreatedUtc` | `DateTime` | Creation timestamp |

Seeded marketplaces:

- MockMarket
- eBay
- TCGplayer
- Card Kingdom
- PriceCharting
- Cardmarket

### `Listings`

Entity: `Listing`

Purpose: original external listing capture table.

Fields:

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | `Guid` | Primary key |
| `CardId` | `Guid` | FK to `Cards.Id` |
| `CardVariantId` | `Guid?` | Optional FK to `CardVariants.Id` |
| `MarketplaceId` | `Guid` | FK to `Marketplaces.Id` |
| `SourceName` | `string` | Source label |
| `ExternalListingId` | `string` | External listing id |
| `Title` | `string` | Listing title |
| `Price` | `decimal` | Precision 18,2 |
| `ShippingPrice` | `decimal?` | Precision 18,2 |
| `Currency` | `string` | Default `USD` |
| `Condition` | `string` | Default `Unknown` |
| `RawCondition` | `string?` | Raw source condition |
| `ListingUrl` | `string` | External listing URL |
| `ImageUrl` | `string?` | Listing image |
| `IsAuction` | `bool` | Auction flag |
| `AuctionEndsUtc` | `DateTime?` | Auction end |
| `ListedAtUtc` | `DateTime` | External listed date |
| `CapturedAtUtc` | `DateTime` | Ingestion timestamp |
| `IsActive` | `bool` | Active listing flag |
| `RawSourceJson` | `string?` | Raw payload |

Indexes:

- `CardId`
- `MarketplaceId`
- `CapturedAtUtc`
- `SourceName`
- Unique: `MarketplaceId`, `ExternalListingId`

Aggregation relevance:

- This is the shape we probably want for a new catalog-level listing table.
- Missing for mature aggregation: sold/completed status, seller id/rating, quantity, fees, tax, shipping region, scrape/API run id, match confidence, normalized condition enum, source-specific SKU.

### `PriceSnapshots`

Entity: `PriceSnapshot`

Purpose: original listing-based price snapshot table.

Fields:

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | `Guid` | Primary key |
| `CardId` | `Guid` | FK to `Cards.Id` |
| `CardVariantId` | `Guid?` | Optional FK to `CardVariants.Id` |
| `MarketplaceId` | `Guid` | FK to `Marketplaces.Id` |
| `LowestPrice` | `decimal` | Precision 18,2 |
| `AveragePrice` | `decimal` | Precision 18,2 |
| `MedianPrice` | `decimal` | Precision 18,2 |
| `ListingCount` | `int` | Count of listings used |
| `Currency` | `string` | Default `USD` |
| `CapturedAtUtc` | `DateTime` | Snapshot timestamp |

Indexes:

- `CardId`, `CardVariantId`, `CapturedAtUtc`

### `PriceReferenceSnapshots`

Entity: `PriceReferenceSnapshot`

Purpose: original reference-price snapshot table for `Card`.

Fields:

Same price fields as `CatalogPriceReferenceSnapshots`, but keyed by:

- `CardId`
- optional `CardVariantId`

Indexes:

- `CardId`
- `CardVariantId`
- `SourceName`
- `CapturedAtUtc`

### `WatchlistItems`

Entity: `WatchlistItem`

Purpose: user watchlist for older `Card` model.

Fields:

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | `Guid` | Primary key |
| `UserId` | `Guid` | User |
| `CardId` | `Guid` | FK to `Cards.Id` |
| `CardVariantId` | `Guid?` | Optional FK to `CardVariants.Id` |
| `TargetPrice` | `decimal?` | Precision 18,2 |
| `Notes` | `string?` | User notes |
| `CreatedUtc` | `DateTime` | Created date |

Indexes:

- `UserId`
- `CardId`
- Unique: `UserId`, `CardId`, `CardVariantId`

### `PriceAlerts`

Entity: `PriceAlert`

Purpose: user target price alerts for older `Card` model.

Fields:

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | `Guid` | Primary key |
| `UserId` | `Guid` | User |
| `CardId` | `Guid` | FK to `Cards.Id` |
| `CardVariantId` | `Guid?` | Optional FK to `CardVariants.Id` |
| `TargetPrice` | `decimal` | Precision 18,2 |
| `IsActive` | `bool` | Alert active flag |
| `HasTriggered` | `bool` | Trigger state |
| `TriggeredAtUtc` | `DateTime?` | Trigger timestamp |
| `CreatedUtc` | `DateTime` | Created date |

Indexes:

- `UserId`
- `CardId`
- `IsActive`

## Current Provider Interfaces

Provider abstractions already exist and can be reused or extended.

### Generic Provider

`ICardDataProvider`

Fields/properties:

- `SourceName`
- `ProviderType`
- `IsEnabled`
- `HealthCheckAsync`

### Catalog Providers

`ICardCatalogProvider`

- `SearchCardsAsync(query, game, ct)`

Current real catalog import providers:

- Scryfall
- PokemonTCG

### Listing Providers

`IMarketplaceListingProvider`

- `SearchListingsAsync(CardSearchContext context, ct)`

Current implementation:

- Mock marketplace provider
- disabled placeholders for future providers

### Price Reference Providers

`IPriceReferenceProvider`

- `GetPriceReferencesAsync(CardSearchContext context, ct)`

Current implementation:

- Mock price reference provider
- disabled placeholders for future providers

### Catalog-Level Pricing Provider

`IExternalPricingProvider`

- `SourceName`
- `IsEnabled`
- `GetPricesForProductAsync(CatalogProductDto product, IReadOnlyList<ExternalProductMappingDto> mappings, ct)`

This is closer to the desired catalog product direction than the older card-level provider interfaces.

## Current API Surface

### Catalog APIs

Base route: `api/catalog`

- `GET api/catalog/games?primaryOnly={bool}`
- `GET api/catalog/sets?gameId={guid}&gameSlug={slug}&upcoming={bool}&take={int}`
- `GET api/catalog/categories`
- `GET api/catalog/products`
- `GET api/catalog/products/{productId}`
- `GET api/catalog/providers/capabilities`

### Marketplace APIs

Base route: `api/marketplace`

- `GET api/marketplace/home?gameSlug={slug}`
- `GET api/marketplace/featured?gameSlug={slug}&take={int}`
- `GET api/marketplace/trending?gameSlug={slug}&take={int}`

Current marketplace home returns:

- primary games
- categories
- trending products
- featured products
- latest sets
- upcoming sets
- provider capabilities

### Catalog Product Pricing APIs

Base route: `api/catalog/products/{productId}/pricing`

- `GET api/catalog/products/{productId}/pricing/history`
- `POST api/catalog/products/{productId}/pricing/refresh`

### Catalog Import APIs

Base route: `api/catalog/import`

- `POST api/catalog/import/preview`
- `POST api/catalog/import/run`
- `GET api/catalog/import/runs`
- `GET api/catalog/import/runs/{importRunId}`

### Seller Inventory APIs

Base route: `api/seller-inventory`

- `GET api/seller-inventory`
- `POST api/seller-inventory`

### Legacy Card Pricing APIs

These still exist for the older `Card` model.

- `GET api/cards/{cardId}/listings`
- `POST api/cards/{cardId}/refresh-listings`
- `GET api/cards/{cardId}/price-history/listings`
- `GET api/cards/{cardId}/price-history/references`
- `POST api/cards/{cardId}/price-history/capture-listing-snapshot`
- `POST api/cards/{cardId}/price-history/refresh-reference-prices`

## Current Frontend Marketplace Behavior

The marketplace home page currently:

- Uses `api/marketplace/home`.
- Displays game tabs for primary games.
- Shows category tiles.
- Shows trending and featured product cards.
- Groups duplicate-looking homepage products by `gameName`, `setName`, `categoryName`, and `name` for display only.
- Shows a print count badge when grouped.
- Uses product card click and `P2W Details` CTA to open the product detail page.
- Shows one green marketplace offer box using `EstimatedMarketPrice` and `PrimarySourceName`.
- Is visually ready for multiple marketplace offer boxes per product.

The current product detail page is scaffolded in a TCGplayer-inspired layout and should eventually be fed by backend aggregation APIs.

## Important Current Limitations

### 1. Catalog-level live listings do not exist yet

There is `Listings`, but it is tied to legacy `Card`.

A full aggregation system needs a catalog-level equivalent, probably keyed by:

- `CatalogProductId`
- optional `ProductVariantId`
- source/marketplace
- external listing id
- condition
- currency
- price
- shipping
- seller
- source URL
- captured timestamp
- match confidence

### 2. Sold/completed transaction data does not exist yet

For real market intelligence, current listings are not enough. We need sold comps and completed sales.

Potential future table:

- `CatalogMarketSales`
- keyed to catalog product/variant/source/external sale id
- sold price, shipping, sold timestamp, condition, seller, quantity, raw payload

### 3. Analytics/rankings do not exist yet

Current `IsTrending` and `IsFeatured` are product flags.

Future calculated tables/services should support:

- trending products
- high volume products
- high margin products
- price movers
- watchlist movers
- volatility
- spread between marketplaces
- buylist-to-market spread
- seller inventory opportunity scoring

### 4. Variant model is not deep enough for high-quality matching

Current `ProductVariant` has useful flags but lacks some fields that will matter:

- normalized condition
- finish/treatment beyond foil/reverse holo
- printing/run identifiers
- language normalization table
- sealed unit quantity
- UPC/GTIN for sealed
- graded company/grade/cert at market data level
- serialized card number details

### 5. External mappings are product-level only

Some providers will require mapping at:

- catalog product level
- product variant level
- provider SKU level
- marketplace listing/sale level

The current `ExternalProductMappings` table is good for catalog imports but probably not sufficient for marketplace price aggregation by itself.

### 6. Currency/country support is UI-only right now

The frontend has a country/currency selector, but backend aggregation does not yet model:

- exchange rates
- buyer country
- shipping regions
- tax/VAT
- marketplace fee estimates by region

## Recommended Aggregation System Design Direction

The first marketplace aggregation system should probably be a separate .NET worker/service in the same solution:

```text
src/
  P2W.Cards.Api/
  P2W.Cards.Application/
  P2W.Cards.Domain/
  P2W.Cards.Infrastructure/
  P2W.Cards.Worker.Aggregation/
```

Recommended principles:

- Keep canonical product identity in `CatalogProduct`.
- Store raw provider payloads for audit/debug.
- Separate current listings, sold comps, reference prices, and computed analytics.
- Use provider-specific checkpoints and ingestion run records.
- Make all marketplace matching confidence-aware.
- Treat sealed, raw singles, graded, and bulk lots as different matching domains.
- Do ranking/analytics in backend jobs, not in the frontend.
- Let frontend consume already-shaped APIs for cards, charts, rankings, and market rows.

## Suggested Future Tables

These are not currently implemented, but are likely needed.

### `CatalogMarketplaces` or reuse/expand `Marketplaces`

If reusing `Marketplaces`, it may need fields:

- `Id`
- `Name`
- `Slug`
- `BaseUrl`
- `IsActive`
- `SupportsListings`
- `SupportsSoldComps`
- `SupportsBuylist`
- `SupportsReferencePrices`
- `DefaultCurrency`
- `CreatedUtc`
- `UpdatedUtc`

### `CatalogMarketplaceListings`

Purpose: current active listings by source.

Suggested fields:

- `Id`
- `CatalogProductId`
- `ProductVariantId`
- `MarketplaceId`
- `SourceName`
- `ExternalListingId`
- `ExternalSku`
- `Title`
- `Price`
- `ShippingPrice`
- `EffectivePrice`
- `Currency`
- `Condition`
- `RawCondition`
- `Quantity`
- `SellerName`
- `SellerRating`
- `SellerLocation`
- `ListingUrl`
- `ImageUrl`
- `IsAuction`
- `AuctionEndsUtc`
- `ListedAtUtc`
- `CapturedAtUtc`
- `LastSeenUtc`
- `IsActive`
- `MatchConfidence`
- `RawSourceJson`

### `CatalogMarketplaceSales`

Purpose: sold/completed comps.

Suggested fields:

- `Id`
- `CatalogProductId`
- `ProductVariantId`
- `MarketplaceId`
- `SourceName`
- `ExternalSaleId`
- `ExternalListingId`
- `Title`
- `SoldPrice`
- `ShippingPrice`
- `EffectiveSoldPrice`
- `Currency`
- `Condition`
- `RawCondition`
- `Quantity`
- `SellerName`
- `SoldAtUtc`
- `CapturedAtUtc`
- `SaleUrl`
- `ImageUrl`
- `MatchConfidence`
- `RawSourceJson`

### `CatalogMarketPriceSnapshots`

Purpose: normalized price snapshots per product/variant/source/condition.

Suggested fields:

- `Id`
- `CatalogProductId`
- `ProductVariantId`
- `MarketplaceId`
- `SourceName`
- `Condition`
- `Currency`
- `LowestListingPrice`
- `MedianListingPrice`
- `AverageListingPrice`
- `LastSoldPrice`
- `MedianSoldPrice`
- `AverageSoldPrice`
- `ListingCount`
- `SoldCount`
- `CapturedAtUtc`

### `CatalogMarketMetrics`

Purpose: computed ranking and market intelligence.

Suggested fields:

- `Id`
- `CatalogProductId`
- `ProductVariantId`
- `Condition`
- `Currency`
- `WindowStartUtc`
- `WindowEndUtc`
- `WindowName`
- `CurrentMarketPrice`
- `PreviousMarketPrice`
- `PriceChangeAmount`
- `PriceChangePercent`
- `ListingCount`
- `SoldCount`
- `VolumeScore`
- `TrendScore`
- `VolatilityScore`
- `LiquidityScore`
- `SpreadScore`
- `OpportunityScore`
- `ComputedAtUtc`

### `CatalogProviderIngestionRuns`

Purpose: job-level ingestion audit for aggregation.

Suggested fields:

- `Id`
- `SourceName`
- `WorkloadType`
- `StartedUtc`
- `FinishedUtc`
- `Status`
- `RecordsProcessed`
- `RecordsCreated`
- `RecordsUpdated`
- `RecordsSkipped`
- `ErrorCount`
- `CheckpointBefore`
- `CheckpointAfter`
- `Notes`

### `CatalogProviderIngestionErrors`

Purpose: per-record provider errors.

Suggested fields:

- `Id`
- `IngestionRunId`
- `SourceName`
- `WorkloadType`
- `ExternalId`
- `CatalogProductId`
- `ErrorMessage`
- `RawSourceJson`
- `CreatedUtc`

## Customer Retention Features the Aggregation System Should Enable

The eventual aggregation system should feed features that bring users back:

- Watchlist price drops.
- Watchlist restock alerts.
- Market mover alerts.
- Trending by game/category.
- High-volume products.
- High-margin products.
- Buylist arbitrage opportunities.
- Cross-market spread detection.
- New set preorder tracking.
- Price history charts.
- Volatility warnings.
- Deal scores.
- Seller inventory repricing suggestions.
- Portfolio value tracking.
- Condition-specific price intelligence.
- Graded vs raw comparison.
- Sealed product performance over time.

## Design Questions for the Next Session

1. Should aggregation use the existing SQL Server database for all raw and computed market data, or should high-volume raw captures eventually move to a separate store?
2. Should the first worker be a .NET hosted service inside the API, or a separate worker project?
3. Which provider should be first for real marketplace pricing: TCGplayer, eBay sold listings, Card Kingdom, or PriceCharting?
4. Do we want to model source marketplace data as current listings first, sold comps first, or reference prices first?
5. How strict should matching be before data is allowed to influence customer-facing prices?
6. What is the minimum useful price chart for MVP: daily market reference, median active listing, or sold comp median?
7. Should product detail pages show source confidence and last updated timestamps from day one?
8. Should P2W internal inventory be included in market price calculations or displayed separately?
9. What retention loops are most important first: watchlists, alerts, deal discovery, or seller repricing?

