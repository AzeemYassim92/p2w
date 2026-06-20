const els = {
  query: document.querySelector("#query"),
  pages: document.querySelector("#pages"),
  take: document.querySelector("#take"),
  minListing: document.querySelector("#minListing"),
  maxListing: document.querySelector("#maxListing"),
  condition: document.querySelector("#condition"),
  minMarket: document.querySelector("#minMarket"),
  maxMarket: document.querySelector("#maxMarket"),
  minProfit: document.querySelector("#minProfit"),
  minMargin: document.querySelector("#minMargin"),
  minRoi: document.querySelector("#minRoi"),
  runScan: document.querySelector("#runScan"),
  status: document.querySelector("#scanStatus"),
  metrics: document.querySelector("#scanMetrics"),
  title: document.querySelector("#resultTitle"),
  searchUrl: document.querySelector("#searchUrl"),
  deals: document.querySelector("#dealRows"),
  pageDiagnostics: document.querySelector("#pageDiagnostics"),
  rejectionSummary: document.querySelector("#rejectionSummary")
};

els.runScan.addEventListener("click", () => runScan());
renderReady();

async function runScan() {
  setLoading();
  const query = new URLSearchParams({
    query: els.query.value || "Pokemon PSA 10",
    pages: els.pages.value || "1",
    take: els.take.value || "50",
    minListing: els.minListing.value || "10",
    maxListing: els.maxListing.value || "1000",
    condition: els.condition.value || "graded",
    conditionId: els.condition.value === "graded" ? "2750" : "",
    minMarket: els.minMarket.value || "25",
    maxMarket: els.maxMarket.value || "500",
    minProfit: els.minProfit.value || "10",
    minMargin: els.minMargin.value || "10",
    minRoi: els.minRoi.value || "10"
  });

  try {
    const response = await fetch(`/api/scan/ebay-lowest?${query}`, { cache: "no-store" });
    const data = await response.json();
    render(data);
  } catch (error) {
    els.status.textContent = "Sweep failed";
    els.status.className = "provider-pill blocked";
    els.title.textContent = "Sweep failed";
    els.deals.innerHTML = `<div class="empty-state">${escapeHtml(error.message)}</div>`;
  }
}

function renderReady() {
  renderMetrics([
    ["Estimated Cost", "$0.001/page", "Zyte broad sweep"],
    ["Default Pages", els.pages.value, "manual run only"],
    ["Condition", "Graded", "eBay LH_ItemCondition=2750"],
    ["Listing Mode", "Buy Now", "auction sweep is separate"]
  ]);
  els.pageDiagnostics.innerHTML = `<div class="empty-state">No page diagnostics yet.</div>`;
  els.rejectionSummary.innerHTML = `<div class="empty-state">No rejection breakdown yet.</div>`;
}

function setLoading() {
  els.status.textContent = "Sweeping eBay";
  els.status.className = "provider-pill";
  els.title.textContent = "Loading broad eBay sweep";
  els.deals.innerHTML = `<div class="empty-state">Fetching eBay page HTML through Zyte and matching against the local catalog...</div>`;
  els.pageDiagnostics.innerHTML = "";
  els.rejectionSummary.innerHTML = "";
  els.searchUrl.hidden = true;
}

function render(data) {
  const live = data.status === "live";
  els.status.textContent = live ? "Sweep live" : "Blocked";
  els.status.className = `provider-pill ${live ? "live" : "blocked"}`;

  const stats = data.stats || {};
  renderMetrics([
    ["Deals", number(data.dealCount), `${number(data.matchedListingCount)} matched / ${number(stats.broadPsa10PokemonListings)} PSA 10 slabs`],
    ["Parsed", number(data.parsedListingCount), `${number(stats.listingBlocksSeen)} blocks seen`],
    ["Rejected", number(stats.rejectedByBroadRules), topRejectReason(stats.broadRejectionReasons)],
    ["Zyte", money(data.estimatedZyteCost), `${number(data.estimatedZyteRequests)} requests`]
  ]);

  renderSearchUrl(data.searchUrls?.[0]);
  renderDeals(data.results || [], data);
  renderPageDiagnostics(stats.pageDiagnostics || []);
  renderRejections(stats.broadRejectionReasons || {}, stats.broadRejectionSamples || {});
}

function renderSearchUrl(url) {
  if (!url) {
    els.searchUrl.hidden = true;
    return;
  }

  els.searchUrl.href = url;
  els.searchUrl.hidden = false;
}

function renderDeals(rows, data) {
  els.title.textContent = rows.length
    ? `${rows.length} matched buy-now candidate${rows.length === 1 ? "" : "s"}`
    : "No hard-filter deal candidates returned";

  if (!rows.length) {
    els.deals.innerHTML = `
      <div class="empty-state">
        ${escapeHtml(data.errorMessage || "No accepted listing matched the catalog and hard deal filters for this sweep.")}
      </div>`;
    return;
  }

  els.deals.innerHTML = rows.map((row) => dealCard(row)).join("");
}

function dealCard(row) {
  const listing = row.listing || {};
  const match = row.catalogMatch || {};
  const card = match.card || {};
  const signals = row.reviewSignals || [];
  const listingUrl = listing.url || "#";
  const priceChartingUrl = card.priceChartingProductUrl || "#";

  return `
    <article class="deal-card">
      <div class="deal-media">
        ${listing.imageUrl ? `<img src="${escapeAttribute(listing.imageUrl)}" alt="${escapeAttribute(card.cardName || listing.title || "listing image")}" loading="lazy" />` : `<div class="image-placeholder">No image</div>`}
      </div>
      <div class="deal-main">
        <div class="deal-title-row">
          <span class="rank">#${number(row.rank)}</span>
          <strong>${escapeHtml(card.cardName || listing.title || "Matched listing")}</strong>
          <span class="confidence ${classToken(row.confidence)}">${escapeHtml(row.confidence || "Unknown")}</span>
        </div>
        <p class="deal-subtitle">${escapeHtml(card.setName || card.priceChartingConsoleName || "Unknown set")}${card.cardNumber ? ` / #${escapeHtml(card.cardNumber)}` : ""}${card.variantName ? ` / ${escapeHtml(card.variantName)}` : ""}</p>
        <p class="listing-title">${escapeHtml(listing.title || "Untitled listing")}</p>
        <div class="deal-kpis">
          ${kpi("Effective Buy", money(listing.effectivePrice), `${money(listing.listingPrice)} item${Number.isFinite(Number(listing.inboundShippingPrice)) ? ` + ${money(listing.inboundShippingPrice)} ship` : ""}`)}
          ${kpi("Market", money(row.expectedMarketValue), "PriceCharting PSA 10")}
          ${kpi("Net Profit", money(row.netProfit), `${percent(row.netMarginPercent)} margin`)}
          ${kpi("ROI", percent(row.roi), `${percent(row.underMarketPercent)} under market`)}
          ${kpi("1yr Volume", number(card.salesVolumeYearly), "PriceCharting sales volume")}
          ${kpi("Match", number(match.score), (match.reasons || []).join(" / "))}
        </div>
        ${signals.length ? `<div class="review-signals">${signals.map((signal) => `<span>${escapeHtml(signal)}</span>`).join("")}</div>` : ""}
        <div class="deal-actions">
          <a class="external-button" href="${escapeAttribute(listingUrl)}" target="_blank" rel="noreferrer">Open eBay Listing</a>
          ${priceChartingUrl !== "#" ? `<a class="external-button secondary" href="${escapeAttribute(priceChartingUrl)}" target="_blank" rel="noreferrer">PriceCharting</a>` : ""}
        </div>
      </div>
    </article>
  `;
}

function renderPageDiagnostics(items) {
  if (!items.length) {
    els.pageDiagnostics.innerHTML = `<div class="empty-state">No page diagnostics returned.</div>`;
    return;
  }

  els.pageDiagnostics.innerHTML = items.map((item) => `
    <div class="diagnostic-card">
      <strong>Page ${number(item.page)} / ${number(item.htmlLength)} chars</strong>
      <span>${escapeHtml(item.title || "Untitled page")}</span>
      <small>${(item.signals || []).map(escapeHtml).join(" / ") || "no listing signals"}</small>
      <small>${item.looksLikeCaptchaOrBotBlock ? "Bot block suspected" : "No bot block detected"}${item.looksLikeSignIn ? " / sign-in text present" : ""}</small>
    </div>
  `).join("");
}

function renderRejections(reasons, samples) {
  const entries = Object.entries(reasons).sort((a, b) => Number(b[1]) - Number(a[1]));
  if (!entries.length) {
    els.rejectionSummary.innerHTML = `<div class="empty-state">No rejected listings returned.</div>`;
    return;
  }

  els.rejectionSummary.innerHTML = entries.map(([reason, count]) => `
    <details class="rejection-card">
      <summary><strong>${escapeHtml(reason)}</strong><span>${number(count)} rows</span></summary>
      <div class="rejection-samples">
        ${(samples[reason] || []).map((sample) => rejectionSample(sample)).join("") || `<small>No sample titles returned.</small>`}
      </div>
    </details>
  `).join("");
}

function rejectionSample(sample) {
  const url = sample.url || "#";
  return `
    <div class="rejection-sample">
      <a href="${escapeAttribute(url)}" target="_blank" rel="noreferrer">${escapeHtml(sample.title || "Untitled")}</a>
      <span>${money(sample.listingPrice)} / ${(sample.reasons || []).map(escapeHtml).join(" / ")}</span>
    </div>
  `;
}

function kpi(label, value, note) {
  return `
    <div class="deal-kpi">
      <span>${escapeHtml(label)}</span>
      <strong>${escapeHtml(value)}</strong>
      <small>${escapeHtml(note || "")}</small>
    </div>
  `;
}

function topRejectReason(reasons) {
  const entries = Object.entries(reasons || {}).sort((a, b) => Number(b[1]) - Number(a[1]));
  return entries.length ? `${entries[0][0]}: ${number(entries[0][1])}` : "no classifier rejections";
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

function percent(value) {
  if (!Number.isFinite(Number(value))) return "-";
  return `${Number(value).toLocaleString(undefined, { maximumFractionDigits: 1 })}%`;
}

function number(value) {
  if (!Number.isFinite(Number(value))) return "0";
  return Number(value).toLocaleString(undefined, { maximumFractionDigits: 1 });
}

function classToken(value) {
  return String(value || "").toLowerCase().replace(/[^a-z0-9]+/g, "-");
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

