import { request } from './http';
import type { CardSet, CatalogPriceSnapshot, CatalogProduct, CatalogProductDetail, Game, MarketplaceHome, SellerInventoryItem } from '../types/catalog';

export function getMarketplaceHome(gameSlug?: string) {
  const suffix = gameSlug ? `?gameSlug=${encodeURIComponent(gameSlug)}` : '';
  return request<MarketplaceHome>(`/api/marketplace/home${suffix}`);
}

export function getCatalogGames(primaryOnly = false) {
  const search = new URLSearchParams();
  if (primaryOnly) search.set('primaryOnly', 'true');
  return request<Game[]>(`/api/catalog/games?${search.toString()}`);
}

export function getCatalogSets(params: { gameId?: string; gameSlug?: string; upcoming?: boolean; take?: number }) {
  const search = new URLSearchParams();
  if (params.gameId) search.set('gameId', params.gameId);
  if (params.gameSlug) search.set('gameSlug', params.gameSlug);
  if (typeof params.upcoming === 'boolean') search.set('upcoming', String(params.upcoming));
  if (params.take) search.set('take', String(params.take));
  return request<CardSet[]>(`/api/catalog/sets?${search.toString()}`);
}

export function getCatalogProducts(params: { gameSlug?: string; categorySlug?: string; cardSetId?: string; productType?: string; query?: string; take?: number }) {
  const search = new URLSearchParams();
  if (params.gameSlug) search.set('gameSlug', params.gameSlug);
  if (params.categorySlug) search.set('categorySlug', params.categorySlug);
  if (params.cardSetId) search.set('cardSetId', params.cardSetId);
  if (params.productType) search.set('productType', params.productType);
  if (params.query) search.set('query', params.query);
  if (params.take) search.set('take', String(params.take));
  return request<CatalogProduct[]>(`/api/catalog/products?${search.toString()}`);
}

export function getCatalogProductDetail(productId: string) {
  return request<CatalogProductDetail>(`/api/catalog/products/${productId}`);
}

export function getCatalogPricingHistory(productId: string) {
  return request<CatalogPriceSnapshot[]>(`/api/catalog/products/${productId}/pricing/history`);
}

export function refreshCatalogPricing(productId: string) {
  return request<void>(`/api/catalog/products/${productId}/pricing/refresh`, { method: 'POST' });
}

export function getSellerInventory() {
  return request<SellerInventoryItem[]>('/api/seller-inventory');
}
