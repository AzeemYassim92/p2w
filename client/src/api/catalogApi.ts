import { request } from './http';
import type { CatalogPriceSnapshot, CatalogProduct, CatalogProductDetail, MarketplaceHome, SellerInventoryItem } from '../types/catalog';

export function getMarketplaceHome(gameSlug?: string) {
  const suffix = gameSlug ? `?gameSlug=${encodeURIComponent(gameSlug)}` : '';
  return request<MarketplaceHome>(`/api/marketplace/home${suffix}`);
}

export function getCatalogProducts(params: { gameSlug?: string; categorySlug?: string; query?: string; take?: number }) {
  const search = new URLSearchParams();
  if (params.gameSlug) search.set('gameSlug', params.gameSlug);
  if (params.categorySlug) search.set('categorySlug', params.categorySlug);
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
