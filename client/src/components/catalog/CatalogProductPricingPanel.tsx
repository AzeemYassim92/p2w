import { useEffect, useState } from 'react';
import { getCatalogPricingHistory, refreshCatalogPricing, type CatalogPriceSnapshot } from '../../api/catalogAdminApi';
import { money } from '../../utils/money';

export function CatalogProductPricingPanel({ productId }: { productId: string }) {
  const [history, setHistory] = useState<CatalogPriceSnapshot[]>([]);

  async function load() {
    setHistory(await getCatalogPricingHistory(productId));
  }

  useEffect(() => { void load(); }, [productId]);

  async function refresh() {
    await refreshCatalogPricing(productId);
    await load();
  }

  return (
    <section>
      <h2>Catalog Pricing</h2>
      <button onClick={refresh}>Refresh Mock Pricing</button>
      <table>
        <thead><tr><th>Source</th><th>Market</th><th>Low</th><th>High</th><th>Captured</th></tr></thead>
        <tbody>
          {history.map((row) => (
            <tr key={row.catalogPriceReferenceSnapshotId}>
              <td>{row.sourceName}</td>
              <td>{money(row.marketPrice, row.currency)}</td>
              <td>{money(row.lowPrice, row.currency)}</td>
              <td>{money(row.highPrice, row.currency)}</td>
              <td>{new Date(row.capturedAtUtc).toLocaleString()}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
