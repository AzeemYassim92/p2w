import { type FormEvent, useEffect, useMemo, useState } from 'react';
import { getCatalogGames, getCatalogSets } from '../api/catalogApi';
import { getDeals } from '../api/marketApi';
import type { CardSet, Game } from '../types/catalog';
import type { DealOpportunity } from '../types/market';
import { money } from '../utils/money';

export function DealScannerPage({ onOpenProduct }: { onOpenProduct: (productId: string) => void }) {
  const [games, setGames] = useState<Game[]>([]);
  const [sets, setSets] = useState<CardSet[]>([]);
  const [deals, setDeals] = useState<DealOpportunity[]>([]);
  const [gameSlug, setGameSlug] = useState('pokemon');
  const [cardSetId, setCardSetId] = useState('');
  const [cardQuery, setCardQuery] = useState('');
  const [threshold, setThreshold] = useState(15);
  const [minMarketValue, setMinMarketValue] = useState('');
  const [maxListingPrice, setMaxListingPrice] = useState('');
  const [minRoiPercent, setMinRoiPercent] = useState('');
  const [minConfidence, setMinConfidence] = useState(55);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    void getCatalogGames(true)
      .then((nextGames) => setGames(Array.isArray(nextGames) ? nextGames : []))
      .catch(() => setGames([]));
  }, []);

  useEffect(() => {
    setCardSetId('');
    void getCatalogSets({ gameSlug, upcoming: false, take: 30 })
      .then((nextSets) => setSets(Array.isArray(nextSets) ? nextSets : []))
      .catch(() => setSets([]));
  }, [gameSlug]);

  useEffect(() => {
    void scanDeals();
  }, []);

  async function scanDeals(event?: FormEvent) {
    event?.preventDefault();
    setIsLoading(true);
    setError('');
    try {
      const nextDeals = await getDeals({
        gameSlug,
        cardSetId: cardSetId || undefined,
        query: cardQuery.trim() || undefined,
        take: 75,
        thresholdPercent: threshold,
        minMarketValue: toNumber(minMarketValue),
        maxListingPrice: toNumber(maxListingPrice),
        minRoiPercent: toNumber(minRoiPercent),
        minConfidence
      });
      setDeals(Array.isArray(nextDeals) ? nextDeals : []);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Deals failed to load');
    } finally {
      setIsLoading(false);
    }
  }

  const summary = useMemo(() => {
    const expectedValue = deals.reduce((sum, deal) => sum + (deal.expectedMarketValue ?? deal.trustedMarketPrice ?? 0), 0);
    const netProfit = deals.reduce((sum, deal) => sum + (estimateNetProfit(deal) ?? 0), 0);
    const averageRoi = deals.length ? deals.reduce((sum, deal) => sum + (estimateRoi(deal) ?? 0), 0) / deals.length : 0;
    return { expectedValue, netProfit, averageRoi };
  }, [deals]);

  return (
    <main className="market-page">
      <section className="marketplace-header">
        <div>
          <p className="eyebrow">Deal scout</p>
          <h1>Listing Opportunities</h1>
          <p>Filter live marketplace rows by set, value, margin, and confidence before deciding what to buy for inventory.</p>
        </div>
      </section>

      <form className="deal-scout-filters" onSubmit={(event) => void scanDeals(event)}>
        <label>
          Game
          <select value={gameSlug} onChange={(event) => setGameSlug(event.target.value)}>
            {games.length === 0 ? <option value="pokemon">Pokemon</option> : null}
            {games.map((game) => <option key={game.gameId} value={game.slug}>{game.name}</option>)}
          </select>
        </label>
        <label>
          Set
          <select value={cardSetId} onChange={(event) => setCardSetId(event.target.value)}>
            <option value="">All recent sets</option>
            {sets.map((set) => (
              <option key={set.cardSetId} value={set.cardSetId}>{set.name}{set.code ? ` / ${set.code}` : ''}</option>
            ))}
          </select>
        </label>
        <label>
          Card
          <input value={cardQuery} onChange={(event) => setCardQuery(event.target.value)} placeholder="Name or number" />
        </label>
        <label>
          Min value
          <input value={minMarketValue} onChange={(event) => setMinMarketValue(event.target.value)} inputMode="decimal" placeholder="$" />
        </label>
        <label>
          Max buy
          <input value={maxListingPrice} onChange={(event) => setMaxListingPrice(event.target.value)} inputMode="decimal" placeholder="$" />
        </label>
        <label>
          Min margin
          <input value={minRoiPercent} onChange={(event) => setMinRoiPercent(event.target.value)} inputMode="decimal" placeholder="ROI %" />
        </label>
        <label>
          Discount
          <select value={threshold} onChange={(event) => setThreshold(Number(event.target.value))}>
            <option value={10}>10%+</option>
            <option value={15}>15%+</option>
            <option value={25}>25%+</option>
            <option value={40}>40%+</option>
          </select>
        </label>
        <label>
          Confidence
          <select value={minConfidence} onChange={(event) => setMinConfidence(Number(event.target.value))}>
            <option value={0}>Any</option>
            <option value={55}>55%+</option>
            <option value={70}>70%+</option>
            <option value={85}>85%+</option>
          </select>
        </label>
        <button type="submit" disabled={isLoading}>{isLoading ? 'Scanning...' : 'Scan Deals'}</button>
      </form>

      {error && <p className="error">{error}</p>}

      <section className="deal-scout-summary">
        <span><strong>{deals.length}</strong> opportunities</span>
        <span><strong>{money(summary.expectedValue)}</strong> expected market value</span>
        <span><strong>{money(summary.netProfit)}</strong> estimated net profit</span>
        <span><strong>{summary.averageRoi.toFixed(1)}%</strong> average ROI</span>
      </section>

      <section className="deal-scout-table" aria-label="Deal Scout opportunities">
        <div className="deal-scout-row deal-scout-head">
          <span>Listing</span>
          <span>Match</span>
          <span>Cost</span>
          <span>Expected</span>
          <span>Profit</span>
          <span>Signal</span>
        </div>
        {deals.length === 0 ? (
          <div className="market-loading">No matching opportunities yet. Refresh a set, lower thresholds, or broaden the set filter.</div>
        ) : (
          deals.map((deal) => (
            <article className="deal-scout-row" key={deal.listingId ?? `${deal.catalogProductId}-${deal.sourceName}-${deal.title}`}>
              <div className="deal-listing-cell">
                {deal.imageUrl ? <img src={deal.imageUrl} alt="" /> : <span className="deal-image-placeholder" />}
                <div>
                  <strong>{deal.title}</strong>
                  <small>{deal.sourceName}{deal.listingUrl ? ' / live listing' : ''}</small>
                </div>
              </div>
              <button className="deal-match-cell" onClick={() => onOpenProduct(deal.catalogProductId)}>
                <strong>{friendlyName(deal.productName)}</strong>
                <small>{deal.setName ?? deal.gameName}{deal.cardNumber ? ` / #${deal.cardNumber}` : ''}</small>
                <span>{formatPercent(normalizeConfidence(deal.matchConfidence))} confidence</span>
              </button>
              <div>
                <strong>{money(deal.listingPrice)}</strong>
                <small>{money(deal.itemPrice)} item / {money(deal.shippingPrice ?? 0)} ship</small>
              </div>
              <div>
                <strong>{money(deal.expectedMarketValue ?? deal.trustedMarketPrice)}</strong>
                <small>{money(estimateFees(deal))} est. fees</small>
              </div>
              <div>
                <strong className={(estimateNetProfit(deal) ?? 0) >= 0 ? 'positive' : 'negative'}>{money(estimateNetProfit(deal))}</strong>
                <small>{formatPercent(estimateRoi(deal))} ROI</small>
              </div>
              <div className="deal-signal-cell">
                <strong>{deal.dealLabel}</strong>
                <small>{formatPercent(deal.discountPercent)} under / liquidity {formatScore(deal.liquidityScore)}</small>
                <p>{deal.reason ?? 'Matched against the current stored market value.'}</p>
                {deal.listingUrl ? <a href={deal.listingUrl} target="_blank" rel="noreferrer">Open Source</a> : null}
              </div>
            </article>
          ))
        )}
      </section>
    </main>
  );
}

function toNumber(value: string) {
  if (!value.trim()) return undefined;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : undefined;
}

function normalizeConfidence(value?: number) {
  if (typeof value !== 'number' || !Number.isFinite(value)) return undefined;
  return value <= 1 ? value * 100 : value;
}

function estimateFees(deal: DealOpportunity) {
  const expected = deal.expectedMarketValue ?? deal.trustedMarketPrice;
  if (typeof deal.estimatedFees === 'number') return deal.estimatedFees;
  if (typeof expected !== 'number') return undefined;
  return (expected * 0.1325) + 0.4;
}

function estimateNetProfit(deal: DealOpportunity) {
  if (typeof deal.estimatedNetProfit === 'number') return deal.estimatedNetProfit;
  const expected = deal.expectedMarketValue ?? deal.trustedMarketPrice;
  const fees = estimateFees(deal);
  if (typeof expected !== 'number' || typeof fees !== 'number') return undefined;
  return expected - fees - deal.listingPrice;
}

function estimateRoi(deal: DealOpportunity) {
  if (typeof deal.estimatedRoiPercent === 'number') return deal.estimatedRoiPercent;
  const netProfit = estimateNetProfit(deal);
  if (typeof netProfit !== 'number' || deal.listingPrice <= 0) return undefined;
  return (netProfit / deal.listingPrice) * 100;
}

function formatPercent(value?: number) {
  return typeof value === 'number' && Number.isFinite(value) ? `${value.toFixed(1)}%` : '-';
}

function formatScore(value?: number) {
  return typeof value === 'number' && Number.isFinite(value) ? `${Math.round(value)}` : '-';
}

function friendlyName(name: string) {
  if (/^_+'s\s+Pikachu/i.test(name)) return 'Birthday Pikachu';
  return name.replace(/^_+('s)/, 'Trainer$1');
}
