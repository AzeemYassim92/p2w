import type { PriceSnapshot } from '../../types/pricing';
import { money } from '../../utils/money';

export function PriceHistoryChart({ snapshots }: { snapshots: PriceSnapshot[] }) {
  return (
    <section>
      <h2>Listing History</h2>
      {snapshots.length === 0 ? <p>No snapshots yet.</p> : snapshots.slice(0, 6).map((item) => (
        <div className="history-row" key={`${item.capturedAtUtc}-${item.lowestPrice}`}>
          <span>{new Date(item.capturedAtUtc).toLocaleString()}</span>
          <span>{money(item.lowestPrice)} low · {money(item.averagePrice)} avg · {item.listingCount} listings</span>
        </div>
      ))}
    </section>
  );
}
