import { useEffect, useState } from 'react';
import { getMarketProviderHealth } from '../api/marketApi';
import type { ProviderHealth } from '../types/market';

export function ProviderHealthPage() {
  const [providers, setProviders] = useState<ProviderHealth[]>([]);
  const [error, setError] = useState('');

  useEffect(() => {
    setError('');
    void getMarketProviderHealth()
      .then(setProviders)
      .catch((err) => setError(err instanceof Error ? err.message : 'Provider health failed to load'));
  }, []);

  return (
    <main className="market-page">
      <section className="marketplace-header">
        <div>
          <p className="eyebrow">Market operations</p>
          <h1>Provider Health</h1>
          <p>Review configured providers, disabled scaffolds, and ingestion readiness.</p>
        </div>
      </section>

      {error && <p className="error">{error}</p>}

      <div className="provider-health-grid">
        {providers.map((provider) => (
          <article className="provider-card" key={`${provider.sourceName}-${provider.providerType}`}>
            <div className="provider-title">
              <strong>{provider.sourceName}</strong>
              <span className={provider.isHealthy ? 'status-on' : 'status-off'}>{provider.status}</span>
            </div>
            <p>{provider.providerType}</p>
            <p>{provider.message}</p>
          </article>
        ))}
      </div>
    </main>
  );
}
