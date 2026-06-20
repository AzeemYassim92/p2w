# Zyte + eBay Search Strategy

Last updated: 2026-06-18

## Role

Zyte/eBay is not the broad catalog or valuation source. It is the expensive verification layer used after PriceCharting narrows the candidate pool.

## Current Strategy

- Start from PriceCharting candidates with PSA 10 value, yearly volume, and target price range.
- Search eBay with PSA 10, Pokemon, exact product name, and card number when present.
- Use eBay low-to-high sort so page 1 is closest to the cheapest effective buy price.
- Calculate effective buy price as listing price plus inbound shipping.
- Reject obvious false positives: PSA 9, BGS, CGC, SGC, raw, ungraded, proxy, custom, reprint, digital, metal, lot, bundle, and collection.
- Parse buy-now and auction modes separately.
- Cache short-lived search results locally so repeated UI refreshes do not repeatedly spend Zyte calls.

## Next Optimization

Add a controlled page-2 fallback only when page 1 is noisy:

- Page 1 by default.
- Page 2 only if matched listings are too low or rejection rate is too high.
- Stop once 10-20 valid matches are found.
- Hard cap at 2-3 pages per product.
- Track lowest-price confidence:
  - High: sorted page 1, many valid matches, low rejection rate.
  - Medium: sorted page 1, some valid matches, moderate noise.
  - Low: few matches, high rejection rate, or no fallback checked.

This keeps cost bounded while making the displayed lowest price more defensible.

## One-Card Probe

Endpoint:

```http
GET /api/probe/ebay?mode=buyNow&take=25
```

Default probe card:

- PriceCharting product: `6362957`
- Product: `Charizard #39`
- Set: `Pokemon 2020 Battle Academy`
- Market basis: PSA 10, `$113.45`

Current search URL strategy:

- Query: card name + card number + set name + Pokemon + PSA 10.
- Listing type: buy-now or auction as separate calls.
- Sort: price plus shipping lowest first, `_sop=15`.
- Page size: `_ipg=240` to maximize rows per Zyte extraction.
- Location: US preferred, `LH_PrefLoc=2`.

The strict matcher requires:

- PSA 10 in the title.
- Card name token match.
- Card number as a token, not a substring inside a cert number.
- At least one non-year set token when a set is known.
- No non-English language keywords.
- No raw, ungraded, non-PSA graders, custom, proxy, reprint, metal, lot, bundle, or collection keywords.

## Probe Baseline: Charizard #39 Battle Academy

One optimized buy-now request produced:

- Requested page size: 240.
- Listing blocks seen: 242.
- Parsed listings: 240.
- Matched listings after strict rules: 5.
- Lowest effective buy-now: `$118.75` (`$110.00` item + `$8.75` shipping).

This means one Zyte call can inspect a full low-to-high eBay results page, but strict matching is mandatory because broad Charizard searches include many wrong variants.

## Cost Guardrails

Observed dashboard baseline from 2026-06-17: about `104` Zyte requests cost roughly `$0.100165`, or about `$0.00096` per request.

Practical estimate:

- 1 product, buy-now only: about 1 request, roughly `$0.001`.
- 100 products, buy-now only: about 100 requests, roughly `$0.10`.
- 100 products, buy-now + auction: about 200 requests, roughly `$0.20`.
- Page-2 fallback doubles cost only for noisy products that need it.

Cost should be controlled with candidate pre-filtering from PriceCharting, short-lived local caching, and page-2 fallback only when page 1 has too few valid matches or a very high rejection rate.
## Broad Lowest-Price Sweep

Endpoint:

```http
GET /api/scan/ebay-lowest?pages=1&take=50&minListing=10&maxListing=250&condition=graded&conditionId=2750
```

Purpose:

- Search eBay broadly for `Pokemon PSA 10`.
- Sort by price plus shipping lowest first.
- Default to eBay graded-card condition filtering with `condition=graded`, which maps to `LH_ItemCondition=2750`.
- Request up to 240 rows per eBay result page.
- Apply eBay listing-price filters with `minListing` and `maxListing` so broad sweeps stay inside the target buy range before Zyte parses the page.
- Match parsed rows back to `dbo.PokemonMasterCatalog`.
- Calculate effective buy price, estimated fees/costs, net profit, margin, ROI, and confidence.

Default hard filters:

- Market value range: `$25` to `$500`.
- Listing price range: `$10` to `$250`.
- Minimum profit: `$10`.
- Minimum margin: `10%`.
- Minimum ROI: `10%`.
- Minimum catalog match score: `75`.

Diagnostics:

- `stats.broadRejectionReasons` groups rejected eBay rows by classifier reason, such as raw grading-potential language, non-English, missing PSA, wrong PSA grade, raw/ungraded, other grader, replica/digital, metal, or bundle/sealed signals.
- `stats.broadRejectionSamples` includes up to five title samples per rejection reason so we can debug over-strict rules without spending extra Zyte calls.
- `ebayCondition` and `ebayConditionId` echo the eBay condition filter used for the sweep; `graded` currently maps to `LH_ItemCondition=2750`.
- `stats.pageDiagnostics` reports returned HTML length, page title, listing signals, and captcha/sign-in hints when Zyte succeeds but the parser finds zero listing rows.
- HTML extraction prefers the Zyte response with stronger eBay listing markers, so browser-rendered HTML wins over raw response HTML when it contains the actual search results.
- Deal confidence is based on identification quality. Thin PriceCharting volume, extreme ROI, and deep under-market spread are emitted as `reviewSignals` for human validation instead of automatically lowering confidence.

Cost guardrail:

- `pages` equals estimated Zyte requests.
- Based on observed usage, `1` page is roughly `$0.001`, `10` pages is roughly `$0.01`, and `100` pages is roughly `$0.10`.
- The endpoint clamps `pages` to `1-100` per request.

Recommended test progression:

1. Start with `pages=1`.
2. If parsing/matching quality looks good, try `pages=10`.
3. Only run `pages=100` after reviewing match quality and false positives.
## eBay Lowest UI

Route:

```http
GET /ebaylowest
```

This page wraps `/api/scan/ebay-lowest` in a manual-run dashboard. It shows Zyte request cost, parser diagnostics, rejection buckets, rejection sample URLs/prices, matched eBay listing links, local catalog match data, PriceCharting URLs, PSA 10 market value, yearly volume, estimated cost, profit, margin, ROI, confidence, and review signals.

The page is intentionally manual-run because every broad sweep consumes Zyte requests.
