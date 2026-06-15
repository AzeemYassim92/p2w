import { useEffect, useState } from 'react';
import { getCatalogWatchlistIntelligence } from '../api/marketApi';
import type { WatchlistIntelligence } from '../types/market';
import { money } from '../utils/money';

export function CatalogWatchlistIntelligencePage({ onOpenProduct }: { onOpenProduct: (productId: string) => void }) {
  const [items, setItems] = useState<WatchlistIntelligence[]>([]);
  const [error, setError] = useState('');

  useEffect(() => {
    setError('');
    void getCatalogWatchlistIntelligence()
      .then(setItems)
      .catch((err) => setError(err instanceof Error ? err.message : 'Watchlist intelligence failed to load'));
  }, []);

  return (
    <main className="market-page">
      <section className="marketplace-header">
        <div>
          <p className="eyebrow">Market intelligence</p>
          <h1>Catalog Watchlist</h1>
          <p>Track target prices, opportunity scores, and refreshed market signals for catalog products.</p>
        </div>
      </section>

      {error && <p className="error">{error}</p>}

      {items.length === 0 ? (
        <section className="empty-panel">No catalog watchlist items yet.</section>
      ) : (
        <div className="watch-intel-list">
          {items.map((item) => (
            <button key={item.catalogWatchlistItemId} onClick={() => onOpenProduct(item.catalogProductId)}>
              <strong>{item.productName}</strong>
              <span>{item.gameName}{item.setName ? ` / ${item.setName}` : ''}</span>
              <b>{money(item.currentMarketPrice)}</b>
              <small>Target {money(item.targetPrice)} | Opportunity {item.opportunityScore?.toFixed(0) ?? '-'}</small>
            </button>
          ))}
        </div>
      )}
    </main>
  );
}
