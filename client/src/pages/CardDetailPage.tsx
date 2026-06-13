import { useEffect, useState } from 'react';
import { getCardDetail } from '../api/cardsApi';
import { getListings, refreshListings } from '../api/listingsApi';
import { captureListingSnapshot, getListingHistory, getReferenceHistory, refreshReferencePrices } from '../api/pricingApi';
import { TargetPriceAlertForm } from '../components/alerts/TargetPriceAlertForm';
import { CardDetailHeader } from '../components/cards/CardDetailHeader';
import { ListingComparisonTable } from '../components/listings/ListingComparisonTable';
import { PriceHistoryChart } from '../components/pricing/PriceHistoryChart';
import { PriceReferencePanel } from '../components/pricing/PriceReferencePanel';
import { WatchlistButton } from '../components/watchlist/WatchlistButton';
import type { CardDetail } from '../types/cards';
import type { Listing } from '../types/listings';
import type { PriceReferenceSnapshot, PriceSnapshot } from '../types/pricing';

export function CardDetailPage({ cardId }: { cardId: string }) {
  const [card, setCard] = useState<CardDetail>();
  const [listings, setListings] = useState<Listing[]>([]);
  const [references, setReferences] = useState<PriceReferenceSnapshot[]>([]);
  const [history, setHistory] = useState<PriceSnapshot[]>([]);

  async function load() {
    setCard(await getCardDetail(cardId));
    setListings(await getListings(cardId));
    setReferences(await getReferenceHistory(cardId));
    setHistory(await getListingHistory(cardId));
  }

  async function refreshAll() {
    await refreshListings(cardId);
    await refreshReferencePrices(cardId);
    await captureListingSnapshot(cardId);
    await load();
  }

  useEffect(() => { void load(); }, [cardId]);

  if (!card) return <main>Loading...</main>;

  return (
    <main>
      <CardDetailHeader card={card} />
      <div className="toolbar">
        <button onClick={refreshAll}>Refresh Listings</button>
        <WatchlistButton cardId={cardId} />
        <TargetPriceAlertForm cardId={cardId} />
      </div>
      <ListingComparisonTable listings={listings} />
      <PriceReferencePanel references={references} />
      <PriceHistoryChart snapshots={history} />
    </main>
  );
}
