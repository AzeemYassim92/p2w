import type { Listing } from '../../types/listings';
import { money } from '../../utils/money';

export function ListingComparisonTable({ listings }: { listings: Listing[] }) {
  return (
    <table>
      <thead>
        <tr><th>Marketplace</th><th>Title</th><th>Condition</th><th>Price</th><th>Total</th></tr>
      </thead>
      <tbody>
        {listings.map((listing) => (
          <tr key={listing.listingId}>
            <td>{listing.marketplaceName}</td>
            <td><a href={listing.listingUrl} target="_blank" rel="noreferrer">{listing.title}</a></td>
            <td>{listing.condition}</td>
            <td>{money(listing.price, listing.currency)}</td>
            <td>{money(listing.effectivePrice, listing.currency)}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
