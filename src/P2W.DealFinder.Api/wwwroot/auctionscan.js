const els = {
  minPrice: document.querySelector("#minPrice"),
  maxPrice: document.querySelector("#maxPrice"),
  minutes: document.querySelector("#minutes"),
  fallbackMinutes: document.querySelector("#fallbackMinutes"),
  minVolume: document.querySelector("#minVolume"),
  candidatePool: document.querySelector("#candidatePool"),
  limit: document.querySelector("#limit"),
  runScan: document.querySelector("#runScan"),
  status: document.querySelector("#scanStatus"),
  metrics: document.querySelector("#scanMetrics"),
  title: document.querySelector("#resultTitle"),
  rows: document.querySelector("#scanRows")
};

els.runScan.addEventListener("click", () => runScan());
runScan();

async function runScan() {
  setLoading();
  const query = new URLSearchParams({
    category: "pokemon-cards",
    grade: "psa10",
    min: els.minPrice.value || "1",
    max: els.maxPrice.value || "250",
    minutes: els.minutes.value || "360",
    fallbackMinutes: els.fallbackMinutes.value || "360",
    minYearlyVolume: els.minVolume.value || "0",
    candidatePool: els.candidatePool.value || "10",
    limit: els.limit.value || "10"
  });

  try {
    const response = await fetch(`/api/scan/auctions?${query}`, { cache: "no-store" });
    const data = await response.json();
    render(data);
  } catch (error) {
    els.status.textContent = "Auction scan failed";
    els.status.className = "provider-pill blocked";
    els.title.textContent = "Auction scan failed";
    els.rows.innerHTML = `<tr><td colspan="9">${escapeHtml(error.message)}</td></tr>`;
  }
}

function setLoading() {
  els.status.textContent = "Scanning ending auctions";
  els.status.className = "provider-pill";
  els.metrics.innerHTML = "";
  els.title.textContent = "Loading ending-soon PSA 10 auctions";
  els.rows.innerHTML = `<tr><td colspan="9">Filtering PriceCharting candidates, then checking eBay auctions ending soon...</td></tr>`;
}

function render(data) {
  const isLive = data.status === "live";
  els.status.textContent = isLive ? "Auction scan live" : "Blocked";
  els.status.className = `provider-pill ${isLive ? "live" : "blocked"}`;

  const rows = data.results || [];
  const marketSpread = rows.reduce((sum, row) => sum + Number(row.spreadToMarket || 0), 0);
  const buySpread = rows.reduce((sum, row) => sum + Number(row.spreadToBuyNow || 0), 0);

  renderMetrics([
    ["Returned", number(data.returnedCount), `${number(data.candidatesSearched)} products searched`],
    ["Window", `${number(data.appliedMinutes)} min`, data.expandedWindow ? `expanded from ${number(data.requestedMinutes)} min` : "requested window"],
    ["Auction Parse", `${number(data.auctionStats?.parsedListings)} / ${number(data.auctionStats?.listingBlocksSeen)}`, `${number(data.auctionStats?.matchedListings)} matched`],
    ["Spread", money(marketSpread), `${money(buySpread)} vs buy-now low`]
  ]);

  if (!isLive) {
    els.title.textContent = "No auction data";
    els.rows.innerHTML = `<tr><td colspan="9">${escapeHtml(data.errorMessage || "Auction scan is unavailable.")}</td></tr>`;
    return;
  }

  els.title.textContent = `English PSA 10 auctions ending within ${number(data.appliedMinutes)} minutes`;
  renderRows(rows);
}

function renderRows(rows) {
  if (!rows.length) {
    els.rows.innerHTML = `<tr><td colspan="9">No matching auctions found in the requested or fallback window.</td></tr>`;
    return;
  }

  els.rows.innerHTML = rows.map((row) => {
    const product = row.product || {};
    const auction = row.auction || {};
    const buyNow = row.lowestBuyNow;
    return `
      <tr>
        <td class="rank">#${row.rank}</td>
        <td>
          <strong>${escapeHtml(product.productName)}</strong>
          <span>${escapeHtml(product.consoleName)}${product.tcgId ? ` / TCG ${escapeHtml(product.tcgId)}` : ""}</span>
        </td>
        <td>
          <strong>${escapeHtml(auction.timeLeftText || `${number(auction.minutesRemaining)} min`)}</strong>
          <span>${number(auction.minutesRemaining)} minutes</span>
        </td>
        <td class="money-cell">${money(product.expectedMarketValue)}</td>
        <td>
          <strong>${number(product.salesVolume)}</strong>
          <span>${number(product.estimatedThirtyDayVolume)} est 30d</span>
        </td>
        <td class="money-cell">${money(auction.effectivePrice)}<span>${money(auction.listingPrice)} bid${auction.inboundShippingPrice ? ` + ${money(auction.inboundShippingPrice)} ship` : ""}</span></td>
        <td class="money-cell">${buyNow ? money(buyNow.effectivePrice) : "-"}<span>${buyNow ? truncate(buyNow.title, 48) : "No buy-now match"}</span></td>
        <td class="money-cell ${Number(row.spreadToMarket) >= 0 ? "positive" : "negative"}">
          ${money(row.spreadToMarket)}
          <span>${number(row.auctionToMarketPercent)}% of market / ${money(row.spreadToBuyNow)} vs BIN</span>
        </td>
        <td>
          <a class="listing-link" href="${escapeAttribute(auction.url)}" target="_blank" rel="noreferrer">Open</a>
          <span title="${escapeAttribute(auction.title)}">${escapeHtml(truncate(auction.title, 64))}</span>
          <span>${number(row.auctionStats?.listingBlocksSeen)} seen / ${number(row.auctionStats?.parsedListings)} parsed / ${number(row.auctionStats?.matchedListings)} matched</span>
        </td>
      </tr>
    `;
  }).join("");
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


