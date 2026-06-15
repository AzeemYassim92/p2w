import { useEffect, useMemo, useState } from 'react';
import { getCatalogGames, getCatalogProducts, getCatalogSets } from '../api/catalogApi';
import { getMarketRankings, getSetMarketDashboard, refreshSetMarketData } from '../api/marketApi';
import type { CardSet, CatalogProduct, Game } from '../types/catalog';
import type { DealOpportunity, MarketAggregationResult, SetMarketDashboard, SetMarketDashboardProduct } from '../types/market';
import { money } from '../utils/money';

type RankingKind = 'trending' | 'high-volume' | 'movers' | 'opportunities' | 'deals';

const rankingTabs: { kind: RankingKind; label: string }[] = [
  { kind: 'trending', label: 'Trending' },
  { kind: 'high-volume', label: 'High Volume' },
  { kind: 'movers', label: 'Movers' },
  { kind: 'opportunities', label: 'Opportunities' },
  { kind: 'deals', label: 'Deals' }
];

export function MarketRankingsPage({ onOpenProduct }: { onOpenProduct: (productId: string) => void }) {
  const [kind, setKind] = useState<RankingKind>('trending');
  const [gameSlug, setGameSlug] = useState('pokemon');
  const [games, setGames] = useState<Game[]>([]);
  const [sets, setSets] = useState<CardSet[]>([]);
  const [selectedSetId, setSelectedSetId] = useState('');
  const [products, setProducts] = useState<CatalogProduct[]>([]);
  const [dashboard, setDashboard] = useState<SetMarketDashboard | null>(null);
  const [rankings, setRankings] = useState<SetMarketDashboardProduct[]>([]);
  const [query, setQuery] = useState('');
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [refreshResult, setRefreshResult] = useState<MarketAggregationResult | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    void getCatalogGames(true)
      .then((nextGames) => setGames(Array.isArray(nextGames) ? nextGames : []))
      .catch(() => setGames([]));
  }, []);

  useEffect(() => {
    setError('');
    setSets([]);
    setSelectedSetId('');
    setProducts([]);
    setDashboard(null);
    setRefreshResult(null);
    void getCatalogSets({ gameSlug: gameSlug || undefined, upcoming: false, take: 30 })
      .then((nextSets) => {
        const safeSets = Array.isArray(nextSets) ? nextSets : [];
        setSets(safeSets);
        setSelectedSetId(safeSets[0]?.cardSetId ?? '');
      })
      .catch((err) => setError(err instanceof Error ? err.message : 'Sets failed to load'));
  }, [gameSlug]);

  useEffect(() => {
    if (!selectedSetId) return;
    void loadSetData(selectedSetId);
  }, [selectedSetId]);

  useEffect(() => {
    setError('');
    void getMarketRankings(kind, gameSlug || undefined, 30)
      .then((rows) => setRankings(Array.isArray(rows) ? rows : []))
      .catch((err) => setError(err instanceof Error ? err.message : 'Rankings failed to load'));
  }, [kind, gameSlug]);

  async function loadSetData(cardSetId: string) {
    setError('');
    try {
      const [nextProducts, nextDashboard] = await Promise.all([
        getCatalogProducts({ cardSetId, take: 1000 }),
        getSetMarketDashboard(cardSetId).catch(() => null)
      ]);
      setProducts(Array.isArray(nextProducts) ? nextProducts : []);
      setDashboard(nextDashboard);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Set market data failed to load');
    }
  }

  async function refreshSelectedSet() {
    if (!selectedSetId) return;
    setIsRefreshing(true);
    setError('');
    try {
      const maxProducts = Math.min(Math.max(products.length || 100, 1), 500);
      const result = await refreshSetMarketData(selectedSetId, maxProducts);
      setRefreshResult(result);
      await loadSetData(selectedSetId);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Set refresh failed');
    } finally {
      setIsRefreshing(false);
    }
  }

  const selectedSet = useMemo(() => sets.find((set) => set.cardSetId === selectedSetId) ?? null, [sets, selectedSetId]);
  const insightRows = useMemo(() => dashboard?.products?.length ? dashboard.products : mergeDashboardRows(dashboard), [dashboard]);
  const insightByProduct = useMemo(() => new Map(insightRows.map((row) => [row.catalogProductId, row])), [insightRows]);
  const filteredProducts = useMemo(() => {
    const term = query.trim().toLowerCase();
    const rows = products.filter((product) => product.isSingleCard);
    if (!term) return rows;
    return rows.filter((product) =>
      product.name.toLowerCase().includes(term)
      || (product.cardNumber ?? '').toLowerCase().includes(term)
      || (product.rarity ?? '').toLowerCase().includes(term));
  }, [products, query]);

  const productsWithMarketData = insightRows.filter((row) => row.hasMarketData).length;
  const activeListings = insightRows.reduce((sum, row) => sum + row.listingCount, 0);
  const soldComps = insightRows.reduce((sum, row) => sum + row.soldCount, 0);
  const bestDeals = dashboard?.bestDeals ?? [];
  const marketRows = rankings.filter((row) => row.hasMarketData);
  const totalSignals = marketRows.reduce((sum, row) => sum + row.listingCount + row.soldCount, 0);

  return (
    <main className="market-page">
      <section className="marketplace-header">
        <div>
          <p className="eyebrow">Market intelligence</p>
          <h1>Market Workstation</h1>
          <p>Start with one set, inspect every card, then drill into sourcing signals and deal candidates.</p>
        </div>
        <div className="market-controls">
          <select value={gameSlug} onChange={(event) => setGameSlug(event.target.value)} aria-label="Game">
            {games.length === 0 ? <option value="pokemon">Pokemon</option> : null}
            {games.map((game) => <option key={game.gameId} value={game.slug}>{game.name}</option>)}
          </select>
          <select value={selectedSetId} onChange={(event) => setSelectedSetId(event.target.value)} aria-label="Set">
            {sets.length === 0 ? <option value="">No sets loaded</option> : null}
            {sets.map((set) => (
              <option key={set.cardSetId} value={set.cardSetId}>
                {set.name}{set.code ? ` / ${set.code}` : ''}
              </option>
            ))}
          </select>
        </div>
      </section>

      {error && <p className="error">{error}</p>}

      <section className="market-workstation-section">
        <div className="market-section-title">
          <div>
            <p className="eyebrow">Selected set</p>
            <h2>{selectedSet?.name ?? 'Most Recent Set'}</h2>
            <span>{selectedSet?.gameName ?? 'Pokemon'}{selectedSet?.releaseDate ? ` / released ${formatDate(selectedSet.releaseDate)}` : ''}</span>
          </div>
          <button className="secondary-button" onClick={() => void refreshSelectedSet()} disabled={!selectedSetId || isRefreshing}>
            {isRefreshing ? 'Refreshing Set...' : 'Refresh Set Market Data'}
          </button>
        </div>

        {refreshResult ? <SetRefreshResult result={refreshResult} /> : null}

        <div className="market-readiness-strip">
          <span><strong>{filteredProducts.length}</strong> cards visible</span>
          <span><strong>{productsWithMarketData}</strong> products with market data</span>
          <span><strong>{activeListings}</strong> active listings</span>
          <span><strong>{soldComps}</strong> sold comps</span>
          <span><strong>{bestDeals.length}</strong> scout candidates</span>
        </div>
      </section>

      <section className="market-workstation-section">
        <div className="market-section-title">
          <div>
            <p className="eyebrow">Marketplace details</p>
            <h2>Set Insights & Buy Signals</h2>
            <span>Recent buy opportunities, activity leaders, low-confidence rows, and chartable market movement.</span>
          </div>
        </div>

        <div className="market-insight-grid">
          <InsightList title="Recent Buy Opportunities" empty="No set-scoped deal candidates yet." deals={bestDeals} onOpenProduct={onOpenProduct} />
          <ProductSignalList title="Activity Leaders" rows={dashboard?.highestVolume ?? []} onOpenProduct={onOpenProduct} />
          <ProductSignalList title="Lowest Confidence" rows={dashboard?.lowestConfidence ?? []} onOpenProduct={onOpenProduct} />
        </div>

        <div className="game-tabs">
          {rankingTabs.map((tab) => (
            <button key={tab.kind} className={kind === tab.kind ? 'active' : ''} onClick={() => setKind(tab.kind)}>
              {tab.label}
            </button>
          ))}
        </div>

        <section className="market-readiness-strip">
          <span><strong>{marketRows.length}</strong> products with market snapshots</span>
          <span><strong>{totalSignals}</strong> listing and sold signals</span>
          <span><strong>{rankings.length - marketRows.length}</strong> refresh candidates</span>
        </section>

        <RankingCharts rows={rankings} />

        {marketRows.length === 0 ? (
          <section className="market-data-callout">
            <strong>No ranked market data yet</strong>
            <p>Refresh a recent set to populate price movement, volume, opportunity, and deal rankings from real provider rows.</p>
          </section>
        ) : null}

        <div className="market-ranking-grid">
          {rankings.map((row, index) => (
            <button key={row.catalogProductId} className={`ranking-card ${row.hasMarketData ? '' : 'ranking-card-empty'}`} onClick={() => onOpenProduct(row.catalogProductId)}>
              <span>#{index + 1}</span>
              <div>
                <strong>{friendlyName(row.productName)}</strong>
                <small>{row.setName ?? row.gameName}{row.cardNumber ? ` / ${row.cardNumber}` : ''}</small>
              </div>
              <div className="ranking-price-row">
                <b>{money(row.currentMarketPrice)}</b>
                <span className={row.hasMarketData ? 'status-on' : 'status-off'}>{row.confidenceLabel}</span>
              </div>
              <p>{row.signalLabel}</p>
              <small>{row.signalDetail}</small>
            </button>
          ))}
        </div>
      </section>

      <section className="market-workstation-section">
        <div className="market-section-title">
          <div>
            <p className="eyebrow">Card listings</p>
            <h2>Set Catalog</h2>
            <span>All cards for the selected set, with stored market coverage beside each row.</span>
          </div>
        </div>

        <div className="set-overview-toolbar">
          <input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Filter cards by name, number, or rarity" />
          <span>{products.length} catalog products in set</span>
        </div>

        <SetOverviewTable products={filteredProducts} insights={insightByProduct} onOpenProduct={onOpenProduct} />
      </section>
    </main>
  );
}

function SetOverviewTable({
  products,
  insights,
  onOpenProduct
}: {
  products: CatalogProduct[];
  insights: Map<string, SetMarketDashboardProduct>;
  onOpenProduct: (productId: string) => void;
}) {
  if (products.length === 0) {
    return <div className="market-loading">No cards found for this set.</div>;
  }

  return (
    <div className="set-overview-table" role="table" aria-label="Set card market overview">
      <div className="set-overview-row set-overview-head" role="row">
        <span>Card</span>
        <span>Market</span>
        <span>Listed</span>
        <span>Sold</span>
        <span>Signal</span>
      </div>
      {products.map((product) => {
        const insight = insights.get(product.catalogProductId);
        return (
          <button key={product.catalogProductId} className="set-overview-row" role="row" onClick={() => onOpenProduct(product.catalogProductId)}>
            <span className="set-card-cell">
              <img src={product.imageUrl} alt="" />
              <span>
                <strong>{friendlyName(product.name)}</strong>
                <small>{product.cardNumber ? `#${product.cardNumber}` : 'No number'}{product.rarity ? ` / ${product.rarity}` : ''}</small>
              </span>
            </span>
            <b>{money(insight?.currentMarketPrice)}</b>
            <span>{insight?.listingCount ?? 0}</span>
            <span>{insight?.soldCount ?? 0}</span>
            <span className={insight?.hasMarketData ? 'status-on' : 'status-off'}>{insight?.confidenceLabel ?? 'Pending'}</span>
          </button>
        );
      })}
    </div>
  );
}

function InsightList({
  title,
  empty,
  deals,
  onOpenProduct
}: {
  title: string;
  empty: string;
  deals: DealOpportunity[];
  onOpenProduct: (productId: string) => void;
}) {
  return (
    <section className="market-insight-card">
      <h3>{title}</h3>
      {deals.length === 0 ? (
        <div className="market-loading compact">{empty}</div>
      ) : (
        <div className="market-signal-list">
          {deals.slice(0, 6).map((deal) => (
            <button key={`${deal.sourceName}-${deal.title}`} onClick={() => onOpenProduct(deal.catalogProductId)}>
              <strong>{friendlyName(deal.productName)}</strong>
              <span>{money(deal.listingPrice)} buy / {money(deal.expectedMarketValue ?? deal.trustedMarketPrice)} market</span>
              <small>{deal.reason ?? `${deal.dealLabel} candidate`}</small>
            </button>
          ))}
        </div>
      )}
    </section>
  );
}

function ProductSignalList({ title, rows, onOpenProduct }: { title: string; rows: SetMarketDashboardProduct[]; onOpenProduct: (productId: string) => void }) {
  return (
    <section className="market-insight-card">
      <h3>{title}</h3>
      {rows.length === 0 ? (
        <div className="market-loading compact">No signal rows yet.</div>
      ) : (
        <div className="market-signal-list">
          {rows.slice(0, 6).map((row) => (
            <button key={`${title}-${row.catalogProductId}`} onClick={() => onOpenProduct(row.catalogProductId)}>
              <strong>{friendlyName(row.productName)}</strong>
              <span>{money(row.currentMarketPrice)} / {row.listingCount} listed / {row.soldCount} sold</span>
              <small>{row.signalDetail || row.confidenceLabel}</small>
            </button>
          ))}
        </div>
      )}
    </section>
  );
}

function SetRefreshResult({ result }: { result: MarketAggregationResult }) {
  const captured = result.listingsCreated + result.listingsUpdated + result.referencePricesCreated + result.snapshotsCreated + result.metricsComputed;
  return (
    <section className={`market-refresh-result ${captured > 0 ? 'success' : 'empty'}`}>
      <strong>Last refresh: {result.status}</strong>
      <span>{result.productsProcessed} processed / {result.productsSkipped} skipped fresh / {result.listingsCreated + result.listingsUpdated} listings / {result.snapshotsCreated} snapshots / {result.metricsComputed} metrics</span>
      {result.notes ? <small>{result.notes}</small> : null}
    </section>
  );
}

function RankingCharts({ rows }: { rows: SetMarketDashboardProduct[] }) {
  const movementRows = rows
    .filter((row) => row.hasMarketData && typeof row.priceChangePercent === 'number')
    .sort((a, b) => Math.abs(b.priceChangePercent ?? 0) - Math.abs(a.priceChangePercent ?? 0))
    .slice(0, 8);
  const volumeRows = rows
    .filter((row) => row.hasMarketData && row.listingCount + row.soldCount > 0)
    .sort((a, b) => (b.listingCount + b.soldCount) - (a.listingCount + a.soldCount))
    .slice(0, 8);

  return (
    <section className="ranking-chart-grid">
      <RankingBarChart
        title="Price Movement"
        subtitle="Largest percent moves from stored market metrics."
        rows={movementRows}
        value={(row) => Math.abs(row.priceChangePercent ?? 0)}
        label={(row) => `${typeof row.priceChangePercent === 'number' ? row.priceChangePercent.toFixed(1) : '0.0'}%`}
      />
      <RankingBarChart
        title="Market Activity"
        subtitle="Active listings plus sold comps used as volume signal."
        rows={volumeRows}
        value={(row) => row.listingCount + row.soldCount}
        label={(row) => `${row.listingCount} listed / ${row.soldCount} sold`}
      />
    </section>
  );
}

function RankingBarChart({
  title,
  subtitle,
  rows,
  value,
  label
}: {
  title: string;
  subtitle: string;
  rows: SetMarketDashboardProduct[];
  value: (row: SetMarketDashboardProduct) => number;
  label: (row: SetMarketDashboardProduct) => string;
}) {
  const max = Math.max(...rows.map(value), 1);

  return (
    <section className="ranking-chart-card">
      <div>
        <h2>{title}</h2>
        <p>{subtitle}</p>
      </div>
      {rows.length === 0 ? (
        <div className="market-loading">No chart data yet.</div>
      ) : (
        <div className="ranking-bars">
          {rows.map((row) => (
            <div key={`${title}-${row.catalogProductId}`} className="ranking-bar-row">
              <span>{friendlyName(row.productName)}</span>
              <div><i style={{ width: `${Math.max(6, (value(row) / max) * 100)}%` }} /></div>
              <b>{label(row)}</b>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}

function mergeDashboardRows(dashboard: SetMarketDashboard | null) {
  if (!dashboard) return [];
  const map = new Map<string, SetMarketDashboardProduct>();
  [
    dashboard.topMovers,
    dashboard.highestVolume,
    dashboard.highestOpportunity,
    dashboard.mostListed,
    dashboard.lowestConfidence
  ].forEach((rows) => {
    rows.forEach((row) => {
      const existing = map.get(row.catalogProductId);
      if (!existing || row.listingCount + row.soldCount > existing.listingCount + existing.soldCount) {
        map.set(row.catalogProductId, row);
      }
    });
  });
  return Array.from(map.values());
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
}

function friendlyName(name: string) {
  if (/^_+'s\s+Pikachu/i.test(name)) return 'Birthday Pikachu';
  return name.replace(/^_+('s)/, 'Trainer$1');
}
