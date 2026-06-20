const params = new URLSearchParams(window.location.search);
const source = (params.get("source") || "justtcg").toLowerCase();

const els = {
  providerEyebrow: document.querySelector("#providerEyebrow"),
  cardName: document.querySelector("#cardName"),
  cardSubtitle: document.querySelector("#cardSubtitle"),
  cardImage: document.querySelector("#cardImage"),
  providerStatus: document.querySelector("#providerStatus"),
  identityDetails: document.querySelector("#identityDetails"),
  sourceTitle: document.querySelector("#sourceTitle"),
  refreshButton: document.querySelector("#refreshButton"),
  metricGrid: document.querySelector("#metricGrid"),
  chartTitle: document.querySelector("#chartTitle"),
  chartBadge: document.querySelector("#chartBadge"),
  chart: document.querySelector("#chart"),
  evidenceTitle: document.querySelector("#evidenceTitle"),
  evidenceList: document.querySelector("#evidenceList"),
  tableTitle: document.querySelector("#tableTitle"),
  rows: document.querySelector("#rows")
};

document.querySelectorAll("[data-source-link]").forEach((link) => {
  if (link.dataset.sourceLink === source) link.classList.add("active");
});

els.cardImage.addEventListener("error", () => els.cardImage.classList.add("failed"));
els.refreshButton.addEventListener("click", () => load());

load();

async function load() {
  setLoading();
  const endpoint = source === "pricecharting" ? "/api/productdetails/pricecharting" : "/api/productdetails/justtcg";
  const response = await fetch(endpoint, { cache: "no-store" });
  const data = await response.json();
  render(data);
}

function setLoading() {
  els.providerStatus.textContent = "Loading";
  els.providerStatus.className = "provider-pill";
  els.metricGrid.innerHTML = "";
  els.evidenceList.innerHTML = "";
  els.rows.innerHTML = "";
  els.chart.innerHTML = `<div class="empty-state">Loading provider data...</div>`;
}

function render(data) {
  const card = data.card;
  document.title = `${card.name} - ${sourceLabel(data.source)}`;
  els.providerEyebrow.textContent = sourceLabel(data.source);
  els.cardName.textContent = card.name;
  els.cardSubtitle.textContent = `${card.setName} #${card.printedNumber || card.number}`;
  els.cardImage.src = card.imageUrl;
  els.providerStatus.textContent = statusText(data);
  els.providerStatus.className = `provider-pill ${data.status || "blocked"}`;
  renderIdentity(card);

  if (data.source === "pricecharting") renderPriceCharting(data);
  else renderJustTcg(data);
}

function renderIdentity(card) {
  els.identityDetails.innerHTML = entries({
    Set: card.setName,
    Number: card.number,
    Supertype: card.supertype,
    Subtypes: (card.subtypes || []).join(", "),
    Rarity: titleCase(card.rarity),
    Artist: card.artist,
    HP: card.hp,
    Types: chipList(card.types),
    Weaknesses: chipList(card.weaknesses),
    "Retreat Cost": chipList([card.retreatCost])
  });
}

function renderJustTcg(data) {
  els.sourceTitle.textContent = "JustTCG Variant Pricing";
  els.chartTitle.textContent = "Variant Price History";
  els.chartBadge.textContent = "180d";
  els.evidenceTitle.textContent = "JustTCG Evidence";
  els.tableTitle.textContent = "JustTCG Variants";

  if (data.status !== "live") {
    renderMetrics([
      ["Provider Status", titleCase(data.status || "blocked"), data.errorCode || ""],
      ["Current Price", "-", "blocked until API responds"],
      ["Variants", "0", "not loaded"],
      ["History Points", "0", "not loaded"]
    ]);
    renderEvidence({
      Source: "JustTCG",
      Status: data.status,
      "Error Code": data.errorCode,
      "Error Message": data.errorMessage
    });
    renderEmptyChart(data.errorMessage || "No JustTCG data returned.");
    renderRows([]);
    return;
  }

  const variants = data.variants || [];
  const prices = variants.map((v) => v.price).filter((p) => Number.isFinite(p));
  const best = prices.length ? Math.min(...prices) : null;
  const history = variants.flatMap((variant) => variant.history || []);
  const gradedFields = variants.flatMap((variant) => variant.gradedLikeFields || []);

  renderMetrics([
    ["Best Variant Price", money(best), "lowest loaded variant"],
    ["Variants", String(variants.length), "condition and printing rows"],
    ["History Points", String(history.length), "provider chart points"],
    ["Graded Fields", String(gradedFields.length), gradedFields.length ? "detected" : "not detected"]
  ]);

  renderEvidence({
    Source: "JustTCG",
    Status: data.status,
    "Provider Card ID": data.providerCardId,
    "TCGPlayer ID": data.tcgPlayerId,
    "Provider Set": data.providerSet,
    "Provider Number": data.providerNumber,
    Rarity: data.rarity,
    "Captured UTC": new Date(data.capturedAtUtc).toLocaleString()
  });

  renderChart(history, "JustTCG price history");
  renderRows(variants.map((variant) => ({
    title: `${variant.condition} / ${variant.printing}`,
    cells: [
      ["Language", variant.language],
      ["Price", money(variant.price), "price"],
      ["7d", percent(variant.change7d)],
      ["30d", percent(variant.change30d)],
      ["90d", percent(variant.change90d)],
      ["History", String((variant.history || []).length)]
    ]
  })));
}

function renderPriceCharting(data) {
  els.sourceTitle.textContent = "PriceCharting Live Product Pricing";
  els.chartTitle.textContent = "Snapshot Only";
  els.chartBadge.textContent = data.status === "live" ? "live" : "reference";
  els.evidenceTitle.textContent = "PriceCharting Evidence";
  els.tableTitle.textContent = "All PriceCharting Value Fields";

  renderMetrics([
    ["Ungraded", money(data.ungradedPrice), "loose-price"],
    ["PSA 10", money(data.psa10Price), "manual-only-price"],
    ["Sales Volume", data.salesVolume == null ? "-" : Number(data.salesVolume).toLocaleString(), "yearly sales-volume"],
    ["Retail Spread", spread(data.retailLooseBuy, data.retailLooseSell), "retail loose buy/sell"]
  ]);

  renderEvidence({
    Source: "PriceCharting",
    Status: data.status,
    "Product ID": data.productId,
    "TCG ID": data.tcgId,
    Product: data.productName,
    Console: data.consoleName,
    Genre: data.genre,
    "Release Date": data.releaseDate,
    Ungraded: money(data.ungradedPrice),
    "Grade 9": money(data.graded9Price),
    "PSA 10": money(data.psa10Price),
    "BGS 10": money(data.bgs10Price),
    "CGC 10": money(data.cgc10Price),
    "SGC 10": money(data.sgc10Price),
    "New Price": money(data.newPrice),
    "CIB Price": money(data.completeInBoxPrice),
    "Box Only": money(data.boxOnlyPrice),
    "Sales Volume": data.salesVolume == null ? null : Number(data.salesVolume).toLocaleString(),
    "Captured UTC": new Date(data.capturedAtUtc).toLocaleString(),
    "Error Message": data.errorMessage
  });

  renderChart(data.referenceHistory || [], "PriceCharting reference history");
  const rows = [];
  (data.priceRows || []).forEach((row) => rows.push({
    title: row.label,
    cells: [
      ["Price", money(row.price), "price"],
      ["Group", row.group],
      ["API Field", row.field]
    ]
  }));

  if (data.rawFields && data.rawFields.length) {
    rows.push(...data.rawFields.map((field) => ({
      title: field.key,
      cells: [
        ["Raw Value", field.value],
        ["Source", "PriceCharting API"]
      ]
    })));
  }

  renderRows(rows);
}

function renderMetrics(metrics) {
  els.metricGrid.innerHTML = metrics.map(([label, value, note]) => `
    <div class="metric">
      <span>${escapeHtml(label)}</span>
      <strong>${escapeHtml(value ?? "-")}</strong>
      <small>${escapeHtml(note ?? "")}</small>
    </div>
  `).join("");
}

function renderEvidence(values) {
  els.evidenceList.innerHTML = Object.entries(values)
    .filter(([, value]) => value !== null && value !== undefined && value !== "")
    .map(([label, value]) => `<div class="evidence-item"><span>${escapeHtml(label)}</span><strong>${escapeHtml(String(value))}</strong></div>`)
    .join("");
}

function renderRows(rows) {
  if (!rows.length) {
    els.rows.innerHTML = `<div class="empty-state">No provider rows loaded.</div>`;
    return;
  }

  els.rows.innerHTML = rows.map((row) => `
    <div class="row-card">
      <strong>${escapeHtml(row.title)}</strong>
      ${row.cells.map(([label, value, cls]) => `
        <div>
          <span>${escapeHtml(label)}</span>
          <div class="${cls || ""}">${escapeHtml(value ?? "-")}</div>
        </div>
      `).join("")}
    </div>
  `).join("");
}

function renderChart(points, label) {
  const normalized = (points || [])
    .map((point, index) => ({
      x: point.unixTime || point.t || index,
      y: Number(point.price)
    }))
    .filter((point) => Number.isFinite(point.y));

  if (normalized.length < 2) {
    renderEmptyChart("No chartable history returned for this source yet.");
    return;
  }

  const width = 760;
  const height = 320;
  const pad = 42;
  const minY = Math.min(...normalized.map((point) => point.y));
  const maxY = Math.max(...normalized.map((point) => point.y));
  const minX = Math.min(...normalized.map((point) => point.x));
  const maxX = Math.max(...normalized.map((point) => point.x));
  const spanY = Math.max(maxY - minY, 1);
  const spanX = Math.max(maxX - minX, 1);
  const coords = normalized.map((point) => {
    const x = pad + ((point.x - minX) / spanX) * (width - pad * 2);
    const y = height - pad - ((point.y - minY) / spanY) * (height - pad * 2);
    return `${x.toFixed(1)},${y.toFixed(1)}`;
  }).join(" ");
  const area = `${pad},${height - pad} ${coords} ${width - pad},${height - pad}`;

  els.chart.innerHTML = `
    <svg role="img" aria-label="${escapeHtml(label)}" viewBox="0 0 ${width} ${height}">
      <rect width="${width}" height="${height}" fill="#f8fafc"></rect>
      <line x1="${pad}" y1="${pad}" x2="${pad}" y2="${height - pad}" stroke="#cbd5e1" />
      <line x1="${pad}" y1="${height - pad}" x2="${width - pad}" y2="${height - pad}" stroke="#cbd5e1" />
      <line x1="${pad}" y1="${pad + 70}" x2="${width - pad}" y2="${pad + 70}" stroke="#e2e8f0" />
      <line x1="${pad}" y1="${height - pad - 70}" x2="${width - pad}" y2="${height - pad - 70}" stroke="#e2e8f0" />
      <polygon points="${area}" fill="rgba(24,169,153,0.12)"></polygon>
      <polyline points="${coords}" fill="none" stroke="#16a34a" stroke-width="4" stroke-linecap="round" stroke-linejoin="round"></polyline>
      <text x="12" y="${pad}" fill="#334155" font-size="14" font-weight="800">${money(maxY)}</text>
      <text x="12" y="${height - pad}" fill="#334155" font-size="14" font-weight="800">${money(minY)}</text>
    </svg>
  `;
}

function renderEmptyChart(message) {
  els.chart.innerHTML = `<div class="empty-state">${escapeHtml(message)}</div>`;
}

function entries(map) {
  return Object.entries(map).map(([key, value]) => `
    <div>
      <dt>${escapeHtml(key)}</dt>
      <dd>${value}</dd>
    </div>
  `).join("");
}

function chipList(values) {
  return (values || []).map((value) => `<span class="chip">${escapeHtml(value)}</span>`).join(" ");
}

function money(value) {
  if (!Number.isFinite(Number(value))) return "-";
  return Number(value).toLocaleString(undefined, { style: "currency", currency: "USD" });
}

function spread(buy, sell) {
  if (!Number.isFinite(Number(buy)) || !Number.isFinite(Number(sell))) return "-";
  return `${money(buy)} / ${money(sell)}`;
}

function percent(value) {
  if (!Number.isFinite(Number(value))) return "-";
  return `${Number(value).toFixed(2)}%`;
}

function sourceLabel(value) {
  return value === "pricecharting" ? "PriceCharting" : "JustTCG";
}

function statusText(data) {
  if (data.status === "live") return `${sourceLabel(data.source)} live`;
  if (data.status === "reference") return `${sourceLabel(data.source)} reference`;
  if (data.status === "reference-with-error") return `${sourceLabel(data.source)} reference fallback`;
  if (data.errorCode === "quota-blocked") return "Quota blocked";
  return `${sourceLabel(data.source)} blocked`;
}

function titleCase(value) {
  return String(value || "")
    .replace(/-/g, " ")
    .replace(/\b\w/g, (match) => match.toUpperCase());
}

function escapeHtml(value) {
  return String(value ?? "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#039;");
}