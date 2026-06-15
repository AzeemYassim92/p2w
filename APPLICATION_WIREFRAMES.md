# P2W Cards Application Wireframes

Current date: 2026-06-15.

These are text wireframes for the current application. They are meant to help reason about layout and product direction, not to be final visual designs.

## Global Shell

```text
+----------------------------------------------------------------------------------+
| P2W Collectibles | Search cards, sealed products, sets...        | Search | US/USD |
| Marketplace | Browse | Sell | Market | Deals | Sets | Import | Mappings | Providers |
+----------------------------------------------------------------------------------+
| Current page content                                                              |
+----------------------------------------------------------------------------------+
```

Current notes:

- Header exists and uses the black/gold theme.
- Search routes to the legacy search page but does not yet pass the query deeply into catalog search.
- Country/currency selector is visual state only.
- Wishlist routes to catalog watchlist intelligence.
- Cart, login, and signup are still scaffolds.
- Import/mappings/providers are currently visible in the main nav but should eventually become admin-only.

## Marketplace Home

```text
+----------------------------------------------------------------------------------+
| Banner image: P2W Collectibles Marketplace                                       |
+----------------------------------------------------------------------------------+
| Trading Card Catalog                                                             |
| P2W Collectibles Marketplace                         [Sell Inventory] [Alerts]   |
| Browse singles, packs, boxes, and upcoming sets.                                  |
+----------------------------------------------------------------------------------+
| [All] [Magic] [Pokemon] [One Piece]                                               |
+----------------------------------------------------------------------------------+
| Shop by Category                                                                  |
| [Singles] [Graded] [Raw Singles] [Foils] [Reverse Holos] [Promos] [Sealed]       |
| [Booster Boxes] [ETBs] [Bundles] [Starter Decks]                                  |
+----------------------------------------------------------------------------------+
| Trending Now                                                                      |
| [Card tile] [Card tile] [Card tile] [Card tile] [Card tile]                       |
|   image                                                                            |
|   game/category                                                                    |
|   product name                                                                     |
|   set/rarity/number chips                                                          |
|   [P2W Details]                                                                    |
|   [$ price | source]                                                               |
+----------------------------------------------------------------------------------+
| Latest Sets                                                                       |
| [Set] [Set] [Set] [Set] [Set]                                                     |
+----------------------------------------------------------------------------------+
| Upcoming Sets                                                                     |
| [Set] [Set] [Set]                                                                 |
+----------------------------------------------------------------------------------+
| Provider Readiness                                                                |
| [TCGplayer] [Scryfall] [PokemonTCG] [eBay] [MTGJSON] [PriceCharting]             |
+----------------------------------------------------------------------------------+
```

Current notes:

- Visual design is much improved and should remain dark-mode shell with white product cards.
- Card image hover zoom exists.
- Product cards link into internal details/pricing.
- Prices are still early and should become provider-aware offer boxes.

## Product Detail Page

```text
+----------------------------------------------------------------------------------+
| Filters: [All Filters] [Condition] [Printing] [Language] [Clear Filters]          |
+----------------------------------------------------------------------------------+
| Breadcrumbs                                                                       |
+----------------------------------------------------------------------------------+
| [Large card image] | Product title                                                |
|                    | Set/subtitle                                                |
|                    | Product Details / Legality tabs                              |
|                    | Rules/metadata fields                                       |
|                    | [Product Details] [Set Market]                               |
|                    |                         [Buy box / listing summary]         |
+----------------------------------------------------------------------------------+
| Market Price History                   | Comparison Prices                         |
| Chart                                   | Price Points                              |
|                                         | 3 Month Snapshot                          |
+----------------------------------------------------------------------------------+
```

Current notes:

- The layout is modeled loosely after TCGplayer product pages.
- Product metadata is still too shallow.
- Legality is scaffolded.
- Buy/cart controls are visual only.
- Rich game-specific fields are the next data-quality priority.

## Product Pricing Page

```text
+----------------------------------------------------------------------------------+
| Breadcrumbs: Marketplace / Sets / Set Name / Product Pricing                      |
+----------------------------------------------------------------------------------+
| [thumbnail] Product name                                                          |
|             Game / Set / #                                                        |
|             [Product Details] [Set Market]                   Current View card    |
+----------------------------------------------------------------------------------+
| Market Intelligence                                                               |
| Aggregated Pricing                                           [Refresh Real Sources]|
| Last refresh status / Diagnostics                                                 |
+----------------------------------------------------------------------------------+
| [Market Price] [Active Listings] [Sales Volume] [Confidence]                      |
+----------------------------------------------------------------------------------+
| Price History chart                         | Comparison Prices                    |
|                                             | Price Points                         |
|                                             | 3 Month Snapshot                     |
+----------------------------------------------------------------------------------+
| Provider Comparison                         | Data Quality                         |
+----------------------------------------------------------------------------------+
| Deal Signals                                                                      |
+----------------------------------------------------------------------------------+
```

Current notes:

- This should become the primary market intelligence/detail page.
- It now receives PokemonTCG reference prices and eBay active listings.
- It still needs sold comps, stricter confidence, outlier filtering, and clearer source status.

## Market Rankings Page

```text
+----------------------------------------------------------------------------------+
| Market Rankings                                                                   |
| Products with the strongest mix of movement, activity, and opportunity. [Game]    |
+----------------------------------------------------------------------------------+
| Set Insights and Buy Signals                                                      |
| [Recent Buy Opportunities] [Activity Leaders] [Lowest Confidence]                 |
+----------------------------------------------------------------------------------+
| [Trending] [High Volume] [Movers] [Opportunities] [Deals]                         |
| [products with snapshots] [listing/sold signals] [refresh candidates]             |
+----------------------------------------------------------------------------------+
| Price Movement chart                         | Market Activity chart                |
+----------------------------------------------------------------------------------+
| Ranking cards / insight cards                                                     |
+----------------------------------------------------------------------------------+
| Card Listings                                                                     |
| Set Catalog                                                                        |
| Search/filter toolbar                                                              |
| [image] Product | Market | Listed | Sold | Confidence                              |
| [image] Product | Market | Listed | Sold | Confidence                              |
| [image] Product | Market | Listed | Sold | Confidence                              |
+----------------------------------------------------------------------------------+
```

Current notes:

- Card listings were moved below analytics so the useful panels are visible first.
- The API now returns all set dashboard product rows so the table is not limited to top subsets.
- Existing running API must be restarted to see the new DTO shape.

## Deal Scanner Page

```text
+----------------------------------------------------------------------------------+
| Deal Scout                                                                        |
| Filters: game / set / card / value / margin / ROI / confidence                    |
+----------------------------------------------------------------------------------+
| Listing opportunities                                                             |
| [image] listing title                                                             |
|        listing price + shipping                                                   |
|        matched product + confidence                                               |
|        expected market value                                                      |
|        estimated fees                                                             |
|        net profit / ROI                                                           |
|        liquidity score                                                            |
|        reason flagged                                                             |
+----------------------------------------------------------------------------------+
```

Current notes:

- This is the right product direction for inventory buying.
- Needs better provider coverage, sold comps, fee assumptions, and listing match explainability.

## Set Dashboard Page

```text
+----------------------------------------------------------------------------------+
| Set Market Dashboard                                                              |
| Select game / set                                                                 |
+----------------------------------------------------------------------------------+
| Set overview                                                                      |
| Products, refreshed rows, products with reference price, products with listings    |
+----------------------------------------------------------------------------------+
| Set Insights and Buy Signals                                                      |
+----------------------------------------------------------------------------------+
| Market analytics                                                                  |
+----------------------------------------------------------------------------------+
| Set catalog rows                                                                  |
+----------------------------------------------------------------------------------+
```

Current notes:

- This should become the main workflow for scanning an entire set.
- We need clearer coverage metrics before users trust the results.

## Import Page

```text
+----------------------------------------------------------------------------------+
| Catalog Import                                                                    |
| [Provider] [Type] [Max records] [Use checkpoint] [Save next] [Checkpoint override]|
| [Dry Run] [Run Import]                                                            |
+----------------------------------------------------------------------------------+
| Last Result / Current checkpoint / Next checkpoint                                |
+----------------------------------------------------------------------------------+
| Recent Runs table                                                                 |
+----------------------------------------------------------------------------------+
```

Current notes:

- Works for catalog metadata imports.
- Should become admin-only.
- Needs import run drilldown, cancellation, retry, and missing-data reports.

## Mapping Review Page

```text
+----------------------------------------------------------------------------------+
| Mapping Review                                                                    |
| Filters: source / game / status / confidence                                      |
+----------------------------------------------------------------------------------+
| Mapping rows                                                                      |
| External product -> catalog product -> confidence -> approve/reject/notes         |
+----------------------------------------------------------------------------------+
```

Current notes:

- Needed for low-confidence matches and provider cleanup.
- Should become admin-only.

## Provider Health Page

```text
+----------------------------------------------------------------------------------+
| Provider Health                                                                   |
| [Provider] [Type] [Enabled] [Healthy] [Status] [Message]                          |
+----------------------------------------------------------------------------------+
```

Current notes:

- Useful for admin/provider debugging.
- Should eventually show yield: attempted, matched, priced, listed, sold comps, no-data reason.

## Watchlist Intelligence

```text
+----------------------------------------------------------------------------------+
| Wishlist / Watchlist Intelligence                                                 |
| Product rows with target price, market price, movement, trend, last updated       |
+----------------------------------------------------------------------------------+
```

Current notes:

- Catalog watchlist exists.
- Needs product detail watchlist buttons and alert creation from watched products.

## Seller Inventory

```text
+----------------------------------------------------------------------------------+
| Seller Inventory                                                                  |
| Inventory rows / future listing creation                                          |
+----------------------------------------------------------------------------------+
```

Current notes:

- Scaffolded.
- Needs real seller listing form, edit/delete, image handling, status lifecycle, and account/auth integration.
