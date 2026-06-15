import { request } from './http';
import type {
  DealOpportunity,
  MarketAggregationResult,
  MarketChart,
  MarketConfidence,
  MarketplaceComparison,
  ProductMarketSummary,
  ProviderHealth,
  SetMarketDashboard,
  SetMarketDashboardProduct,
  WatchlistIntelligence
} from '../types/market';

export function getProductMarketSummary(productId: string) {
  return request<ProductMarketSummary>(`/api/market/products/${productId}/summary`);
}

export function getProductMarketConfidence(productId: string) {
  return request<MarketConfidence>(`/api/market/products/${productId}/confidence`);
}

export function getMarketplaceComparison(productId: string, condition = 'NearMint') {
  const search = new URLSearchParams({ condition, currency: 'USD' });
  return request<MarketplaceComparison>(`/api/market/products/${productId}/comparison?${search.toString()}`);
}

export function getMarketChart(productId: string, range = '1y') {
  const search = new URLSearchParams({ range, condition: 'NearMint', currency: 'USD' });
  return request<MarketChart>(`/api/market/products/${productId}/chart?${search.toString()}`);
}

export function getProductDeals(productId: string, take = 6) {
  return request<DealOpportunity[]>(`/api/market/products/${productId}/deals?take=${take}`);
}

export type DealSearchParams = {
  gameSlug?: string;
  cardSetId?: string;
  catalogProductId?: string;
  query?: string;
  take?: number;
  thresholdPercent?: number;
  minMarketValue?: number;
  maxListingPrice?: number;
  minRoiPercent?: number;
  minConfidence?: number;
};

export function getDeals(params: DealSearchParams = {}) {
  const search = new URLSearchParams({
    take: String(params.take ?? 25),
    thresholdPercent: String(params.thresholdPercent ?? 15)
  });
  if (params.gameSlug) search.set('gameSlug', params.gameSlug);
  if (params.cardSetId) search.set('cardSetId', params.cardSetId);
  if (params.catalogProductId) search.set('catalogProductId', params.catalogProductId);
  if (params.query) search.set('query', params.query);
  if (typeof params.minMarketValue === 'number') search.set('minMarketValue', String(params.minMarketValue));
  if (typeof params.maxListingPrice === 'number') search.set('maxListingPrice', String(params.maxListingPrice));
  if (typeof params.minRoiPercent === 'number') search.set('minRoiPercent', String(params.minRoiPercent));
  if (typeof params.minConfidence === 'number') search.set('minConfidence', String(params.minConfidence));
  return request<DealOpportunity[]>(`/api/market/deals?${search.toString()}`);
}

export function refreshProductMarketData(productId: string, useMock = false) {
  return request<MarketAggregationResult>(`/api/market/aggregation/products/${productId}/refresh`, {
    method: 'POST',
    body: JSON.stringify({ force: true, useMockData: useMock, maxProducts: 1 })
  });
}

export function refreshSetMarketData(cardSetId: string, maxProducts = 100, useMock = false, force = false) {
  return request<MarketAggregationResult>(`/api/market/aggregation/sets/${cardSetId}/refresh`, {
    method: 'POST',
    body: JSON.stringify({ force, useMockData: useMock, maxProducts })
  });
}

export function getMarketRankings(kind: 'trending' | 'high-volume' | 'movers' | 'opportunities' | 'deals', gameSlug?: string, take = 25) {
  const search = new URLSearchParams({ take: String(take) });
  if (gameSlug) search.set('gameSlug', gameSlug);
  return request<SetMarketDashboardProduct[]>(`/api/market/rankings/${kind}?${search.toString()}`);
}

export function getSetMarketDashboardBySlug(gameSlug: string, setSlug: string) {
  return request<SetMarketDashboard>(`/api/market/sets/by-slug/${encodeURIComponent(gameSlug)}/${encodeURIComponent(setSlug)}/dashboard`);
}

export function getSetMarketDashboard(cardSetId: string) {
  return request<SetMarketDashboard>(`/api/market/sets/${cardSetId}/dashboard`);
}

export function getCatalogWatchlistIntelligence() {
  return request<WatchlistIntelligence[]>('/api/catalog-watchlist/intelligence');
}

export function addCatalogWatchlistItem(productId: string, targetPrice?: number) {
  return request<WatchlistIntelligence>('/api/catalog-watchlist', {
    method: 'POST',
    body: JSON.stringify({ catalogProductId: productId, targetPrice, alertOnVolumeSpike: true, alertOnPriceDrop: true, alertOnNewDeal: true })
  });
}

export function getMarketProviderHealth() {
  return request<ProviderHealth[]>('/api/market/providers/health');
}
