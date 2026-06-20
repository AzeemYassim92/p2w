const els = {
  minPrice: document.querySelector("#minPrice"),
  maxPrice: document.querySelector("#maxPrice"),
  grade: document.querySelector("#grade"),
  limit: document.querySelector("#limit"),
  runScan: document.querySelector("#runScan"),
  buyNowTab: document.querySelector("#buyNowTab"),
  auctionTab: document.querySelector("#auctionTab"),
  listingHeader: document.querySelector("#listingHeader"),
  status: document.querySelector("#scanStatus"),
  metrics: document.querySelector("#scanMetrics"),
  title: document.querySelector("#resultTitle"),
  rows: document.querySelector("#scanRows")
};

let activeListingMode = "buyNow";
let lastData = null;

els.runScan.addEventListener("click", () => runScan());
els.buyNowTab.addEventListener("click", () => switchListingMode("buyNow"));
els.auctionTab.addEventListener("click", () => switchListingMode("auction"));
runScan();

async function runScan() {
  setLoading();
  const query = new URLSearchParams({
    category: "pokemon-cards",
    grade: els.grade.value,
    min: els.minPrice.value || "10",
    max: els.maxPrice.value || "200",
    limit: els.limit.value || "10",
    includeEbay: "true",
    includeBuyNow: "true",
    includeAuction: "true"
  });

  try {
    const response = await fetch(`/api/scan/pricecharting?${query}`, { cache: "no-store" });
    const data = await response.json();
    lastData = data;
    render(data);
  } catch (error) {
    els.status.textContent = "Scan failed";
    els.status.className = "provider-pill blocked";
    els.title.textContent = "Scan failed";
    els.rows.innerHTML = `<tr><td colspan="9">${escapeHtml(error.message)}</td></tr>`;
  }
}

function switchListingMode(mode) {
  activeListingMode = mode;
  els.buyNowTab.classList.toggle("primary", mode === "buyNow");
  els.auctionTab.classList.toggle("primary", mode === "auction");
  els.listingHeader.textContent = mode === "buyNow" ? "Lowest Found Price" : "Lowest Found Auction";
  if (lastData) render(lastData);
}

function setLoading() {
  els.status.textContent = "Scanning PriceCharting + eBay";
  els.status.className = "provider-pill";
  els.metrics.innerHTML = "";
  els.title.textContent = "Loading candidates";
  els.rows.innerHTML = `<tr><td colspan="9">Downloading PriceCharting data, then checking eBay for the top candidates...</td></tr>`;
}

function render(data) {
  const isLive = data.status === "live";
  els.status.textContent = isLive ? "Scan live" : "Blocked";
  els.status.className = `provider-pill ${isLive ? "live" : "blocked"}`;

  const rows = data.results || [];
  const foundBuyNow = rows.filter((row) => row.lowestBuyNow).length;
  const foundAuctions = rows.filter((row) => row.lowestAuction).length;
  const stats = aggregateStats(rows);
  const yearlyVolume = rows.reduce((sum, row) => sum + Number(row.salesVolume || 0), 0);
  const thirtyDayVolume = rows.reduce((sum, row) => sum + Number(row.estimatedThirtyDayVolume || 0), 0);

  renderMetrics([
    ["Returned", number(data.returnedCount), `${number(data.candidateCount)} matched filters`],
    ["eBay Matches", `${foundBuyNow} buy / ${foundAuctions} auction`, data.ebayStatus || "not requested"],
    ["eBay Parsed", `${number(stats.parsed)} / ${number(stats.seen)}`, `${number(stats.matched)} matched listings`],
    ["Volume", `${number(thirtyDayVolume)} est 30d`, `${number(yearlyVolume)} yearly units`]
  ]);

  if (!isLive) {
    els.title.textContent = "No scan data";
    els.rows.innerHTML = `<tr><td colspan="9">${escapeHtml(data.errorMessage || "PriceCharting scan is unavailable.")}</td></tr>`;
    return;
  }

  els.title.textContent = `${data.grade} candidates from ${money(data.minPrice)} to ${money(data.maxPrice)}`;
  renderRows(data);
}

function renderRows(data) {
  const rows = data.results || [];
  if (!rows.length) {
    els.rows.innerHTML = `<tr><td colspan="9">No candidates matched this range.</td></tr>`;
    return;
  }

  els.rows.innerHTML = rows.map((row) => {
    const listing = activeListingMode === "buyNow" ? row.lowestBuyNow : row.lowestAuction;
    const stats = activeListingMode === "buyNow" ? row.buyNowStats : row.auctionStats;
    const gap = listing && Number.isFinite(Number(row.maxEffectiveBuyForTenPercentMargin))
      ? Number(row.maxEffectiveBuyForTenPercentMargin) - Number(listing.effectivePrice)
      : null;

    return `
      <tr>
        <td class="rank">#${row.rank}</td>
        <td>
          <strong>${escapeHtml(row.productName)}</strong>
          <span>${escapeHtml(row.genre || "Pokemon Card")}${row.tcgId ? ` / TCG ${escapeHtml(row.tcgId)}` : ""}</span>
        </td>
        <td>${escapeHtml(row.consoleName)}${row.releaseDate ? `<span>${escapeHtml(row.releaseDate)}</span>` : ""}</td>
        <td class="money-cell">${money(row.expectedMarketValue)}</td>
        <td>${money(row.ungradedPrice)}</td>
        <td>${volumeCell(row)}</td>
        <td class="money-cell ${Number(row.maxEffectiveBuyForTenPercentMargin) > 0 ? "positive" : "negative"}">${money(row.maxEffectiveBuyForTenPercentMargin)}</td>
        <td>${listingCell(listing, stats)}</td>
        <td class="money-cell ${Number(gap) >= 0 ? "positive" : "negative"}">${money(gap)}</td>
      </tr>
    `;
  }).join("");
}

function listingCell(listing, stats) {
  if (!listing) return `<span class="muted-cell">No match</span>${statsLine(stats)}`;
  const shipping = Number.isFinite(Number(listing.inboundShippingPrice)) ? ` + ${money(listing.inboundShippingPrice)} ship` : "";
  const bids = listing.bidCount ? ` / ${number(listing.bidCount)} bids` : "";
  return `
    <a class="listing-link" href="${escapeAttribute(listing.url)}" target="_blank" rel="noreferrer">${money(listing.effectivePrice)}</a>
    <span>${escapeHtml(listing.listingType)} / ${money(listing.listingPrice)}${shipping}${bids}</span>
    <span title="${escapeAttribute(listing.title)}">${escapeHtml(truncate(listing.title, 62))}</span>
    ${statsLine(stats)}
  `;
}

function volumeCell(row) {
  return `
    <strong>${number(row.estimatedThirtyDayVolume)} est</strong>
    <span>${number(row.salesVolume)} yearly</span>
  `;
}

function statsLine(stats) {
  if (!stats) return "";
  const error = stats.errorMessage ? ` / ${escapeHtml(stats.errorMessage)}` : "";
  const cache = stats.usedCache ? " / cache" : "";
  return `<span>${number(stats.listingBlocksSeen)} seen / ${number(stats.parsedListings)} parsed / ${number(stats.matchedListings)} matched${cache}${error}</span>`;
}

function aggregateStats(rows) {
  return rows.reduce((total, row) => {
    const stats = activeListingMode === "buyNow" ? row.buyNowStats : row.auctionStats;
    if (!stats) return total;
    total.seen += Number(stats.listingBlocksSeen || 0);
    total.parsed += Number(stats.parsedListings || 0);
    total.matched += Number(stats.matchedListings || 0);
    return total;
  }, { seen: 0, parsed: 0, matched: 0 });
}

function renderMetrics(metrics) {
  els.metrics.innerHTML = metrics.map(([label, value, note]) => `
    <div class="metric">
      <span>${escapeHtml(label)}</span>
      <strong>${escapeHtml(value ?? "-")}</strong>
      <small>${escapeHtml(note ?? "")}</small>
    </div>
  `).join("");
}

function money(value) {
  if (!Number.isFinite(Number(value))) return "-";
  return Number(value).toLocaleString(undefined, { style: "currency", currency: "USD" });
}

function number(value) {
  if (!Number.isFinite(Number(value))) return "0";
  return Number(value).toLocaleString(undefined, { maximumFractionDigits: 1 });
}

function truncate(value, length) {
  const text = String(value || "");
  return text.length > length ? `${text.slice(0, length - 1)}...` : text;
}

function escapeHtml(value) {
  return String(value ?? "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#039;");
}

function escapeAttribute(value) {
  return escapeHtml(value);
}
