import { useEffect, useMemo, useState } from 'react';
import {
  getMarketChart,
  getMarketplaceComparison,
  getProductDeals,
  getProductMarketConfidence,
  getProductMarketSummary,
  refreshProductMarketData
} from '../../api/marketApi';
import type {
  DealOpportunity,
  MarketChart,
  MarketConfidence,
  MarketAggregationResult,
  MarketplaceComparison,
  ProductMarketSummary
} from '../../types/market';
import { money } from '../../utils/money';

export function ProductMarketIntelligence({ productId }: { productId: string }) {
  const [summary, setSummary] = useState<ProductMarketSummary | null>(null);
  const [comparison, setComparison] = useState<MarketplaceComparison | null>(null);
  const [chart, setChart] = useState<MarketChart | null>(null);
  const [confidence, setConfidence] = useState<MarketConfidence | null>(null);
  const [deals, setDeals] = useState<DealOpportunity[]>([]);
  const [range, setRange] = useState('1y');
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [lastRefresh, setLastRefresh] = useState<MarketAggregationResult | null>(null);
  const [error, setError] = useState('');

  async function load(nextRange = range) {
    setError('');
    try {
      const [nextSummary, nextComparison, nextChart, nextConfidence, nextDeals] = await Promise.all([
        getProductMarketSummary(productId),
        getMarketplaceComparison(productId),
        getMarketChart(productId, nextRange),
        getProductMarketConfidence(productId),
        getProductDeals(productId, 6)
      ]);
      setSummary(nextSummary);
      setComparison({
        ...nextComparison,
        rows: Array.isArray(nextComparison.rows) ? nextComparison.rows : []
      });
      setChart({
        ...nextChart,
        priceSeries: Array.isArray(nextChart.priceSeries) ? nextChart.priceSeries : [],
        volumeSeries: Array.isArray(nextChart.volumeSeries) ? nextChart.volumeSeries : [],
        distributionBuckets: Array.isArray(nextChart.distributionBuckets) ? nextChart.distributionBuckets : [],
        percentileMarkers: Array.isArray(nextChart.percentileMarkers) ? nextChart.percentileMarkers : []
      });
      setConfidence(nextConfidence);
      setDeals(Array.isArray(nextDeals) ? nextDeals : []);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Market intelligence failed to load');
    }
  }

  async function refresh() {
    setIsRefreshing(true);
    try {
      const result = await refreshProductMarketData(productId);
      setLastRefresh(result);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Market refresh failed');
    } finally {
      setIsRefreshing(false);
    }
  }

  useEffect(() => { void load(); }, [productId]);

  function changeRange(nextRange: string) {
    setRange(nextRange);
    void load(nextRange);
  }

  const confidencePercent = normalizeConfidence(confidence?.score ?? summary?.confidenceScore ?? 0);
  const hasMarketData = Boolean(summary?.hasMarketData || comparison?.rows?.length || chart?.priceSeries?.length);

  return (
    <section className="market-intelligence">
      <div className="market-intelligence-header">
        <div>
          <p className="eyebrow">Market intelligence</p>
          <h2>Aggregated Pricing</h2>
        </div>
        <div className="market-toolbar">
          {summary?.isDemoData || chart?.isDemoData ? <span className="demo-badge">Demo rows hidden</span> : null}
          <button className="secondary-button" onClick={() => void refresh()} disabled={isRefreshing}>{isRefreshing ? 'Refreshing...' : 'Refresh Real Sources'}</button>
        </div>
      </div>

      {error && <p className="error">{error}</p>}

      {lastRefresh ? <RefreshResult result={lastRefresh} /> : null}

      {summary && !hasMarketData ? (
        <section className="market-data-callout market-data-callout-compact">
          <strong>{summary.dataStatus || 'Needs refresh'}</strong>
          <p>{summary.dataQualityMessage || 'No real marketplace/reference rows have been captured for this product yet.'}</p>
        </section>
      ) : null}

      <div className="market-kpis">
        <MetricCard label="Market Price" value={money(summary?.currentMarketPrice)} accent={summary?.priceChangePercent} />
        <MetricCard label="Active Listings" value={summary?.listingCount ?? 0} sub={`${summary?.soldCount ?? 0} sold comps`} />
        <MetricCard label="Sales Volume" value={money(summary?.salesVolume)} sub={summary?.freshnessLabel ?? 'Pending'} />
        <MetricCard label="Confidence" value={`${confidencePercent}%`} sub={confidence?.label ?? summary?.confidenceLabel ?? 'Pending'} />
      </div>

      <div className="market-detail-layout">
        <section className="market-panel market-chart-panel">
          <div className="market-panel-title">
            <h3>Price History</h3>
            <div className="range-tabs">
              {['30d', '90d', '1y'].map((item) => (
                <button key={item} className={range === item ? 'active' : ''} onClick={() => changeRange(item)}>{item}</button>
              ))}
            </div>
          </div>
          {chart ? <MarketLineChart chart={chart} /> : <div className="market-loading">Loading chart...</div>}
        </section>

        <AggregatePricingPanels summary={summary} comparison={comparison} chart={chart} />
      </div>

      <div className="market-detail-layout">
        <section className="market-panel">
          <h3>Provider Comparison</h3>
          <ProviderComparison comparison={comparison} />
        </section>

        <section className="market-panel">
          <h3>Data Quality</h3>
          <dl className="quality-list">
            <div><dt>Included Comps</dt><dd>{summary?.includedComparableCount ?? 0}</dd></div>
            <div><dt>Excluded Comps</dt><dd>{summary?.excludedComparableCount ?? 0}</dd></div>
            <div><dt>Last Updated</dt><dd>{summary?.lastUpdatedUtc ? new Date(summary.lastUpdatedUtc).toLocaleString() : 'Pending'}</dd></div>
            <div><dt>Net Margin</dt><dd>{money(summary?.estimatedNetMargin)}</dd></div>
          </dl>
          {confidence?.notes ? (
            <div className="confidence-notes">
              <span>{confidence.notes}</span>
            </div>
          ) : null}
        </section>
      </div>

      <section className="market-panel">
        <h3>Deal Signals</h3>
        <DealList deals={deals} />
      </section>
    </section>
  );
}

function RefreshResult({ result }: { result: MarketAggregationResult }) {
  const diagnosticEvents = Array.isArray(result.diagnosticEvents) ? result.diagnosticEvents : [];
  const captured =
    result.listingsCreated +
    result.listingsUpdated +
    result.referencePricesCreated +
    result.snapshotsCreated +
    result.metricsComputed;
  const message = captured > 0
    ? [
        `${result.referencePricesCreated} reference price${result.referencePricesCreated === 1 ? '' : 's'}`,
        `${result.listingsCreated + result.listingsUpdated} listing row${result.listingsCreated + result.listingsUpdated === 1 ? '' : 's'}`,
        `${result.snapshotsCreated} snapshot${result.snapshotsCreated === 1 ? '' : 's'}`,
        `${result.metricsComputed} metric${result.metricsComputed === 1 ? '' : 's'}`
      ].join(' | ')
    : 'No usable rows were returned by the enabled real providers for this product.';

  return (
    <section className={`market-refresh-result ${captured > 0 ? 'success' : 'empty'}`}>
      <strong>Last refresh: {result.status}</strong>
      <span>{message}</span>
      {result.notes ? <small>{result.notes}</small> : null}
      {diagnosticEvents.length > 0 ? (
        <details className="market-refresh-diagnostics">
          <summary>Diagnostics ({diagnosticEvents.length})</summary>
          <ol>
            {diagnosticEvents.map((event, index) => (
              <li key={`${event.stage}-${index}`}>
                <b>{event.level}</b>
                <span>{event.stage}</span>
                <p>{event.message}</p>
                {event.data ? <code>{event.data}</code> : null}
              </li>
            ))}
          </ol>
        </details>
      ) : null}
    </section>
  );
}

function AggregatePricingPanels({
  summary,
  comparison,
  chart
}: {
  summary: ProductMarketSummary | null;
  comparison: MarketplaceComparison | null;
  chart: MarketChart | null;
}) {
  const comparisonRows = (comparison?.rows ?? []).filter((row) => hasAnyPrice(row.referenceMarketPrice, row.lowestActiveListing, row.medianActiveListing, row.lastSoldPrice));
  const primaryRows = comparisonRows.slice(0, 3);
  const points = getChartPrices(chart);
  const low = firstNumber(summary?.lowPrice, points.length ? Math.min(...points) : undefined);
  const high = firstNumber(summary?.highPrice, points.length ? Math.max(...points) : undefined);
  const average = points.length ? points.reduce((sum, value) => sum + value, 0) / points.length : undefined;
  const listedMedian = firstNumber(
    comparisonRows.map((row) => row.medianActiveListing ?? row.lowestActiveListing).find((value) => typeof value === 'number'),
    summary?.currentMarketPrice
  );
  const currentQuantity = summary?.listingCount ?? 0;
  const currentSellers = comparisonRows.filter((row) => (row.listingCount ?? 0) > 0).length;
  const totalSold = summary?.soldCount ?? 0;
  const avgDailySold = totalSold > 0 ? totalSold / 90 : undefined;

  return (
    <aside className="aggregate-panel-stack">
      <section className="market-panel aggregate-mini-panel">
        <div className="market-panel-title compact">
          <h3>Comparison Prices</h3>
          <span>Near Mint</span>
        </div>
        {primaryRows.length > 0 ? (
          <div className="comparison-price-grid">
            {primaryRows.map((row) => (
              <div key={row.sourceName}>
                <span>{row.sourceName}</span>
                <strong>{money(row.referenceMarketPrice ?? row.medianActiveListing ?? row.lowestActiveListing ?? row.lastSoldPrice)}</strong>
              </div>
            ))}
          </div>
        ) : (
          <div className="market-loading compact">No comparison prices yet.</div>
        )}
      </section>

      <section className="market-panel aggregate-mini-panel">
        <div className="market-panel-title compact">
          <h3>Price Points</h3>
          <span>Near Mint</span>
        </div>
        <div className="price-point-summary">
          <div>
            <span>Market Price</span>
            <strong>{money(summary?.currentMarketPrice)}</strong>
          </div>
          <MarketRange low={low} market={summary?.currentMarketPrice} high={high} />
          <dl>
            <div><dt>Listed Median</dt><dd>{money(listedMedian)}</dd></div>
            <div><dt>Current Quantity</dt><dd>{currentQuantity}</dd></div>
            <div><dt>Current Sellers</dt><dd>{currentSellers}</dd></div>
            <div><dt>High</dt><dd>{money(high)}</dd></div>
          </dl>
        </div>
      </section>

      <section className="market-panel aggregate-mini-panel">
        <div className="market-panel-title compact">
          <h3>3 Month Snapshot</h3>
          <button className="link-button" type="button">View More Data</button>
        </div>
        <dl className="snapshot-mini-grid">
          <div><dt>Average Sale</dt><dd>{money(average)}</dd></div>
          <div><dt>Low Sale</dt><dd>{money(low)}</dd></div>
          <div><dt>High Sale</dt><dd>{money(high)}</dd></div>
          <div><dt>Total Sold</dt><dd>{totalSold}</dd></div>
          <div><dt>Avg. Daily Sold</dt><dd>{typeof avgDailySold === 'number' ? avgDailySold.toFixed(1) : '-'}</dd></div>
        </dl>
      </section>
    </aside>
  );
}

function MarketRange({ low, market, high }: { low?: number; market?: number; high?: number }) {
  const values = [low, market, high].filter((value): value is number => typeof value === 'number' && Number.isFinite(value));
  const min = values.length ? Math.min(...values) : 0;
  const max = values.length ? Math.max(...values) : 1;
  const spread = max - min || 1;
  const marketPos = typeof market === 'number' ? ((market - min) / spread) * 100 : 50;

  return (
    <div className="market-range">
      <span />
      <i style={{ left: `${Math.max(0, Math.min(100, marketPos))}%` }} />
    </div>
  );
}

function getChartPrices(chart: MarketChart | null) {
  return (chart?.priceSeries ?? [])
    .map((point) => point.medianSoldPrice ?? point.referencePrice ?? point.medianActiveListing ?? point.lowestActiveListing)
    .filter((value): value is number => typeof value === 'number' && Number.isFinite(value));
}

function hasAnyPrice(...values: Array<number | undefined>) {
  return values.some((value) => typeof value === 'number' && Number.isFinite(value));
}

function firstNumber(...values: Array<number | undefined>) {
  return values.find((value): value is number => typeof value === 'number' && Number.isFinite(value));
}

function normalizeConfidence(score: number) {
  return Math.round(score <= 1 ? score * 100 : score);
}

function MetricCard({ label, value, sub, accent }: { label: string; value: string | number; sub?: string; accent?: number | null }) {
  const hasAccent = typeof accent === 'number' && Number.isFinite(accent);
  return (
    <article className="market-kpi-card">
      <span>{label}</span>
      <strong>{value}</strong>
      {hasAccent ? <small className={accent >= 0 ? 'positive' : 'negative'}>{accent.toFixed(1)}%</small> : <small>{sub}</small>}
    </article>
  );
}

function MarketLineChart({ chart }: { chart: MarketChart }) {
  const series = chart.priceSeries ?? [];
  const points = useMemo(() => series.map((point) => ({
    date: point.dateUtc,
    market: point.medianSoldPrice ?? point.referencePrice ?? point.medianActiveListing ?? point.lowestActiveListing,
    low: point.lowPrice,
    high: point.highPrice
  })).filter((point) => point.market !== undefined && point.market !== null), [series]);

  if (points.length === 0) {
    return <div className="market-loading">No price points yet.</div>;
  }

  const values = points.flatMap((point) => [point.market, point.low, point.high]).filter((value): value is number => typeof value === 'number' && Number.isFinite(value));
  const min = Math.min(...values);
  const max = Math.max(...values);
  const spread = max - min || 1;
  const path = points.map((point, index) => {
    const x = points.length === 1 ? 50 : (index / (points.length - 1)) * 100;
    const y = 90 - (((point.market ?? min) - min) / spread) * 76;
    return `${index === 0 ? 'M' : 'L'} ${x.toFixed(2)} ${y.toFixed(2)}`;
  }).join(' ');

  return (
    <div className="market-svg-chart">
      <div className="market-chart-axis">
        <span>{money(max)}</span>
        <span>{money((max + min) / 2)}</span>
        <span>{money(min)}</span>
      </div>
      <svg viewBox="0 0 100 100" preserveAspectRatio="none" role="img" aria-label="Market price history">
        <path className="chart-grid" d="M0 14H100M0 52H100M0 90H100" />
        <path className="chart-line" d={path} />
        {points.map((point, index) => {
          const x = points.length === 1 ? 50 : (index / (points.length - 1)) * 100;
          const y = 90 - (((point.market ?? min) - min) / spread) * 76;
          return <circle key={`${point.date}-${index}`} cx={x} cy={y} r="1.6" />;
        })}
      </svg>
    </div>
  );
}

function ProviderComparison({ comparison }: { comparison: MarketplaceComparison | null }) {
  const rows = comparison?.rows ?? [];
  if (rows.length === 0) {
    return <div className="market-loading">No provider rows yet.</div>;
  }

  return (
    <div className="provider-comparison-table">
      <div className="provider-comparison-head">
        <span>Source</span>
        <span>Reference</span>
        <span>Listing</span>
        <span>Sold</span>
        <span>Signal</span>
      </div>
      {rows.map((row) => (
        <div className="provider-comparison-row" key={row.sourceName}>
          <strong>{row.sourceName}</strong>
          <span>{money(row.referenceMarketPrice)}</span>
          <span>{money(row.lowestActiveListing)} <small>{row.listingCount}</small></span>
          <span>{money(row.lastSoldPrice)} <small>{row.soldCount}</small></span>
          <span>{row.confidenceLabel}</span>
        </div>
      ))}
    </div>
  );
}

function DealList({ deals }: { deals: DealOpportunity[] }) {
  if (deals.length === 0) {
    return <div className="market-loading">No real-provider deal opportunities yet.</div>;
  }

  return (
    <div className="deal-list">
      {deals.map((deal) => (
        <a key={`${deal.sourceName}-${deal.title}`} href={deal.listingUrl || '#'} target="_blank" rel="noreferrer">
          <span>{deal.sourceName}</span>
          <strong>{deal.title}</strong>
          <b>{money(deal.listingPrice)} {typeof deal.discountPercent === 'number' ? `${deal.discountPercent.toFixed(1)}% under` : ''}</b>
        </a>
      ))}
    </div>
  );
}
