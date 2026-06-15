# P2W Collectibles Product TODO

Living checklist for turning the current marketplace scaffold into a fully functional product website.

Last updated: 2026-06-14.

## How To Use This Doc

- Keep this as the shared product backlog while the app evolves.
- Check items off only when the feature works end to end, not just visually.
- Add notes under each section as new ideas come up.
- Prefer splitting large ideas into backend, frontend, data, and testing tasks.

## Buttons And UI Actions Needing Real Functionality

### Global Header

- [ ] `Search`: connect global search text to the marketplace/catalog search query instead of only routing to the legacy search page.
- [ ] `US / USD` country selector: persist selected country/currency.
- [ ] `US / USD` country selector: connect to currency conversion, shipping region, tax/VAT assumptions, and later language.
- [x] `Wishlist`: convert from legacy card watchlist to catalog-product watchlist.
- [ ] `Cart 0`: build cart page/drawer and show real item count.
- [ ] `Login`: replace local fake `loggedIn` state with real auth flow.
- [ ] `Sign Up`: replace local fake `loggedIn` state with real account creation.
- [ ] `Account`: add account menu, profile, orders, seller dashboard, logout.
- [ ] `Marketplace`: route is functional; continue improving marketplace landing experience.
- [ ] `Browse`: replace older card-only browse with catalog-product browsing.
- [ ] `Sell`: connect seller flow to create/edit/list inventory.
- [ ] `Import`: decide whether this remains admin-only and hide from normal users.
- [ ] `Mappings`: decide whether this remains admin-only and hide from normal users.
- [ ] `Alerts`: convert from legacy card alerts to catalog-product alerts.

### Marketplace Home

- [ ] `Sell Inventory`: route to seller inventory/listing creation.
- [ ] `Price Alerts`: route to alert creation or watchlist movers.
- [ ] Game tabs: currently functional; add URL state/deep links.
- [ ] Category tiles: make each tile filter product grid/category browse.
- [ ] Product card image/top-half click: currently opens product detail; verify against all product types.
- [x] `P2W Details`: currently opens product detail; keep as canonical internal detail route.
- [ ] Green marketplace offer boxes: support multiple live sources per product.
- [ ] Green marketplace offer boxes: handle missing price/source gracefully.
- [ ] Card art hover preview: currently works for real images; prevent horizontal page scroll and test edge cases.
- [ ] Trending products: replace manual `IsTrending` flag with backend market analytics.
- [ ] Featured products: decide whether manual merchandising, algorithmic, or hybrid.
- [ ] Latest sets: make set cards clickable and filter products by set.
- [ ] Upcoming sets: make set cards clickable and support preorder views.
- [ ] Provider readiness cards: decide if admin-only or public trust/coverage display.

### Product Detail Page

- [ ] Breadcrumb buttons: route/filter correctly instead of being mostly visual.
- [ ] `All Filters`: open real listing filter panel.
- [ ] `Condition`: filter price/listing/offer data by condition.
- [ ] `Printing`: filter variants/printings.
- [ ] `English`: filter language.
- [ ] `Clear Filters`: reset all detail-page filters.
- [ ] `Product Details` tab: replace fallback details with richer provider metadata and rules text.
- [ ] `Legality` tab: integrate game-specific legality data where available.
- [ ] Quantity selector: connect to cart quantity.
- [ ] `Add to Cart`: create cart item.
- [ ] `Refresh price`: decide whether this is admin/internal, rate-limited user action, or background-only.
- [ ] `View 11 Other Listings`: open listings table/drawer with real marketplace offers.
- [ ] `Sell this`: route to prefilled seller listing form.
- [ ] `Report a problem`: create issue/report flow for bad data, bad image, wrong mapping, or stale price.
- [ ] `View More Data`: open full price history / analytics view.
- [ ] Seller link: route to seller profile/store page when internal sellers exist.
- [ ] Financing copy: remove, replace, or connect to actual payment provider terms.

### Search/Browse Page

- [ ] Replace legacy card search page title and flow with catalog-product search.
- [ ] Product category tabs: map to real `ProductCategory` records.
- [ ] Search query: include catalog products, sets, sealed products, and variants.
- [ ] Game filter: use actual `Games`.
- [ ] Search results: route to catalog product detail, not legacy card detail.
- [ ] Add sorting: relevance, market price, newest, trending, volume, price change.
- [ ] Add filters: game, category, set, rarity, condition, language, foil/reverse, price range.

### Watchlist And Alerts

- [x] Convert watchlist model from legacy `CardId` to `CatalogProductId` and optional `ProductVariantId`.
- [ ] Add watchlist button to marketplace cards.
- [ ] Add watchlist button to product detail page.
- [ ] Add alert creation from product detail page.
- [ ] Add alert types: below target price, restock, price spike/drop, volume spike.
- [ ] Add notification delivery settings: email, SMS, in-app, later push.
- [x] Add watchlist market summary: current price, change, trend, last updated.

### Seller Inventory

- [ ] Create seller listing form.
- [ ] Edit seller inventory item.
- [ ] Delete/archive seller inventory item.
- [ ] Upload/manage seller inventory images.
- [ ] Support condition, grading company, grade, cert number, language, variant, quantity.
- [ ] Add asking price suggestions from market aggregation.
- [ ] Add draft/listed/sold status lifecycle.
- [ ] Add seller dashboard metrics.
- [ ] Add seller profile/store page.

### Admin Import And Mapping

- [ ] Hide admin import/mapping pages from normal users.
- [ ] Add admin auth/role protection for import and mapping routes.
- [ ] Add import run cancellation or safe stop.
- [ ] Add import run detail drilldown.
- [ ] Add import error search/filter.
- [ ] Add mapping review search/filter by source, game, confidence, status.
- [ ] Add bulk mapping actions.
- [ ] Add duplicate/missing mapping reports.
- [ ] Add provider health dashboard.

## Marketplace Aggregation System

### Design

- [ ] Finalize system design for marketplace aggregation.
- [ ] Decide first implementation language/service boundary. Current recommendation: .NET worker in same solution.
- [ ] Decide whether raw market data stays in SQL Server for MVP or moves to a separate store later.
- [ ] Decide first real pricing provider: TCGplayer, eBay sold listings, Card Kingdom, PriceCharting, or another.
- [ ] Define source priority rules and confidence rules.
- [ ] Define refresh cadence by product type and popularity.
- [ ] Define stale data policy and last-updated display rules.

### Data Model

- [x] Add catalog-level live listing table.
- [x] Add catalog-level sold/completed sales table.
- [x] Add catalog-level market price snapshot table.
- [x] Add catalog-level market metrics table.
- [x] Add provider ingestion run/error/checkpoint tables for aggregation.
- [x] Add provider SKU/mapping table at product and variant level.
- [ ] Add provider observation table for successful identity match but missing price/listing/sold payload.
- [ ] Add external set mapping table so display set codes do not get confused with provider set ids.
- [ ] Add per-product provider coverage table/materialized view: identity resolved, reference price present, listings present, sold comps present, last success, last no-data reason.
- [ ] Add normalized condition model for market data.
- [ ] Add currency/exchange-rate model.
- [ ] Add marketplace fee/shipping estimate model.

### Provider Connectors

- [ ] TCGplayer product/price connector.
- [ ] Decide whether PokemonTCG remains identity/catalog-only for customer-facing confidence or can be used as a low-coverage reference source.
- [ ] Add provider yield metrics: attempted products, matched products, priced products, listed products, sold-comp products, zero-row reasons.
- [x] eBay active listing connector scaffold.
- [ ] eBay active listing OAuth token exchange and live Browse API execution.
- [x] eBay sold/completed listing connector disabled scaffold.
- [ ] Card Kingdom retail/buylist connector.
- [ ] PriceCharting connector.
- [ ] Cardmarket connector for international expansion.
- [ ] Provider rate limiting and retries.
- [ ] Provider credential/config management.
- [ ] Provider health checks.

### Matching And Normalization

- [ ] Build catalog-product matching pipeline for listing titles.
- [ ] Build variant matching: language, foil, reverse holo, first edition, promo, serialized.
- [ ] Build sealed product matching: pack, box, case, bundle, ETB, UPC/GTIN where available.
- [ ] Build graded matching: company, grade, cert where available.
- [x] Store match confidence and exclude low-confidence data from customer-facing prices.
- [ ] Create review workflow for low-confidence/high-value mappings.

### Analytics

- [x] Compute current market price by product/variant/condition/source.
- [x] Compute listing median, low, high, average.
- [x] Compute sold comp median, low, high, average.
- [ ] Separate identity confidence from market confidence.
- [ ] Do not compute customer-facing market price from catalog-only identity matches.
- [ ] Compute volume score.
- [ ] Compute trend score.
- [ ] Compute volatility score.
- [ ] Compute liquidity score.
- [ ] Compute marketplace spread.
- [ ] Compute high-margin opportunity score.
- [ ] Compute buylist arbitrage opportunity score.
- [ ] Compute watchlist movers.
- [x] Feed trending products from computed analytics.
- [x] Feed high-volume products from computed analytics.
- [ ] Feed high-margin products from computed analytics.

## Commerce And Checkout

- [ ] Cart model and persistence.
- [ ] Add to cart.
- [ ] Update cart quantity.
- [ ] Remove cart item.
- [ ] Checkout flow.
- [ ] Payment provider selection/integration.
- [ ] Shipping address capture.
- [ ] Shipping rate calculation.
- [ ] Tax calculation.
- [ ] Order creation.
- [ ] Order confirmation page.
- [ ] Order history.
- [ ] Seller order management.
- [ ] Refund/cancel flow.

## Accounts And Trust

- [ ] Real user authentication.
- [ ] Signup verification.
- [ ] Password reset.
- [ ] Account profile.
- [ ] Seller onboarding.
- [ ] Seller verification/KYC requirements if needed.
- [ ] Buyer/seller ratings.
- [ ] Report user/listing/product issue flow.
- [ ] Admin moderation queue.
- [ ] Audit logs for admin actions.

## Catalog Quality

- [ ] Use `PRODUCT_DATA_COMPLETENESS_PLAN.md` as the gating checklist before adding more product-detail or market-intelligence features.
- [ ] Validate PokemonTCG import completion regularly against API `totalCount`.
- [ ] Validate Scryfall import completion against bulk data/counts.
- [ ] Add One Piece real catalog/image provider.
- [ ] Add Yu-Gi-Oh catalog provider.
- [ ] Add Lorcana catalog provider.
- [ ] Add duplicate detection reports by game/set/name/card number.
- [ ] Add missing image reports.
- [ ] Add missing mapping reports.
- [ ] Add stale mapping reports.
- [ ] Add product merge/split tooling.

## Frontend Polish

- [ ] Fix horizontal scroll on marketplace page.
- [ ] Make header responsive on tablet/mobile.
- [ ] Make product cards responsive across zoom levels.
- [ ] Tune banner height and first-screen density.
- [ ] Add loading states/skeletons.
- [ ] Add empty states that explain next action.
- [ ] Add error states with retry where appropriate.
- [ ] Add accessible focus states and keyboard paths.
- [ ] Add image fallback polish per game/product type.
- [ ] Replace placeholder set symbols where possible.
- [ ] Make admin pages visually match the new dark/gold theme or intentionally separate them.

## Backend/API Cleanup

- [ ] Decide migration path from legacy `Card` model to canonical `CatalogProduct` model.
- [ ] Convert legacy listing, watchlist, alert, and search APIs to catalog-product equivalents.
- [ ] Add route-level auth and role authorization.
- [ ] Add API pagination consistently.
- [ ] Add API sorting consistently.
- [ ] Add API filtering consistently.
- [ ] Add API response metadata for last updated/staleness.
- [ ] Add structured error response consistency.
- [ ] Add request validation.
- [ ] Add rate limiting for user-triggered refreshes.

## Testing

- [ ] Unit tests for catalog matching.
- [ ] Unit tests for condition normalization.
- [x] Unit tests for provider parsers.
- [x] Unit tests for market metric calculations.
- [ ] Integration tests for import checkpoints.
- [x] Integration tests for aggregation ingestion runs.
- [ ] API tests for catalog product detail.
- [ ] API tests for marketplace home.
- [ ] API tests for cart/checkout when implemented.
- [ ] Frontend tests for product card routing.
- [ ] Frontend tests for product detail filters.
- [ ] Visual regression checks for marketplace cards and detail page.

## Launch Readiness

- [ ] Environment-specific configuration.
- [ ] Secrets management.
- [ ] Production database migration plan.
- [ ] Background worker deployment plan.
- [ ] Logging and monitoring.
- [ ] Error tracking.
- [ ] Provider quota/rate-limit monitoring.
- [ ] Backup and restore strategy.
- [ ] Data retention policy for raw provider payloads.
- [ ] Terms of service.
- [ ] Privacy policy.
- [ ] Marketplace seller terms.
- [ ] Copyright/licensing review for images and banners.
