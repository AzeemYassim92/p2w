import type { PriceReferenceSnapshot } from '../../types/pricing';
import { money } from '../../utils/money';

export function PriceReferencePanel({ references }: { references: PriceReferenceSnapshot[] }) {
  const latest = references[0];
  return (
    <section>
      <h2>Reference Pricing</h2>
      {latest ? (
        <div className="metrics">
          <span>Market {money(latest.marketPrice)}</span>
          <span>Ungraded {money(latest.ungradedPrice)}</span>
          <span>Grade 9 {money(latest.grade9Price)}</span>
          <span>Grade 10 {money(latest.grade10Price)}</span>
        </div>
      ) : <p>No reference prices yet.</p>}
    </section>
  );
}
