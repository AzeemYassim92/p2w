import { type FormEvent, useState } from 'react';
import { getSetMarketDashboardBySlug } from '../api/marketApi';
import type { SetMarketDashboard, SetMarketDashboardProduct } from '../types/market';
import { money } from '../utils/money';

export function SetMarketDashboardPage({ onOpenProduct }: { onOpenProduct: (productId: string) => void }) {
  const [gameSlug, setGameSlug] = useState('pokemon');
  const [setSlug, setSetSlug] = useState('destined-rivals');
  const [dashboard, setDashboard] = useState<SetMarketDashboard | null>(null);
  const [error, setError] = useState('');

  async function submit(event: FormEvent) {
    event.preventDefault();
    setError('');
    try {
      const nextDashboard = await getSetMarketDashboardBySlug(gameSlug, setSlug);
      setDashboard({
        ...nextDashboard,
        topMovers: Array.isArray(nextDashboard.topMovers) ? nextDashboard.topMovers : [],
        highestVolume: Array.isArray(nextDashboard.highestVolume) ? nextDashboard.highestVolume : [],
        highestOpportunity: Array.isArray(nextDashboard.highestOpportunity) ? nextDashboard.highestOpportunity : [],
        mostListed: Array.isArray(nextDashboard.mostListed) ? nextDashboard.mostListed : [],
        lowestConfidence: Array.isArray(nextDashboard.lowestConfidence) ? nextDashboard.lowestConfidence : [],
        bestDeals: Array.isArray(nextDashboard.bestDeals) ? nextDashboard.bestDeals : []
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Set dashboard failed to load');
    }
  }

  return (
    <main className="market-page">
      <section className="marketplace-header">
        <div>
          <p className="eyebrow">Market intelligence</p>
          <h1>Set Dashboard</h1>
          <p>Inspect movers, volume, opportunities, and low-confidence products by set.</p>
        </div>
        <form className="set-dashboard-form" onSubmit={submit}>
          <input value={gameSlug} onChange={(event) => setGameSlug(event.target.value)} aria-label="Game slug" />
          <input value={setSlug} onChange={(event) => setSetSlug(event.target.value)} aria-label="Set slug" />
          <button type="submit">Load Set</button>
        </form>
      </section>

      {error && <p className="error">{error}</p>}

      {dashboard && (
        <>
          <section className="set-dashboard-title">
            <h2>{dashboard.setName}</h2>
            <span>{dashboard.gameName}</span>
          </section>
          <DashboardSection title="Top Movers" rows={dashboard.topMovers} onOpenProduct={onOpenProduct} />
          <DashboardSection title="Highest Volume" rows={dashboard.highestVolume} onOpenProduct={onOpenProduct} />
          <DashboardSection title="Highest Opportunity" rows={dashboard.highestOpportunity} onOpenProduct={onOpenProduct} />
          <DashboardSection title="Most Listed" rows={dashboard.mostListed} onOpenProduct={onOpenProduct} />
          <DashboardSection title="Lowest Confidence" rows={dashboard.lowestConfidence} onOpenProduct={onOpenProduct} />
        </>
      )}
    </main>
  );
}

function DashboardSection({ title, rows, onOpenProduct }: { title: string; rows: SetMarketDashboardProduct[]; onOpenProduct: (productId: string) => void }) {
  const displayRows = Array.isArray(rows) ? rows : [];
  if (displayRows.length === 0) {
    return null;
  }

  return (
    <section className="set-dashboard-section">
      <h2>{title}</h2>
      <div className="market-ranking-grid">
        {displayRows.map((row) => (
          <button key={`${title}-${row.catalogProductId}`} className="ranking-card" onClick={() => onOpenProduct(row.catalogProductId)}>
            <strong>{row.productName}</strong>
            <b>{money(row.currentMarketPrice)}</b>
            <small>{typeof row.priceChangePercent === 'number' ? `${row.priceChangePercent.toFixed(1)}%` : `${row.listingCount} listed / ${row.soldCount} sold`}</small>
            <span>{row.confidenceLabel}</span>
          </button>
        ))}
      </div>
    </section>
  );
}
