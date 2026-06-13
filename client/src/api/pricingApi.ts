import { request } from './http';
import type { PriceReferenceSnapshot, PriceSnapshot } from '../types/pricing';

export const getListingHistory = (cardId: string) => request<PriceSnapshot[]>(`/api/cards/${cardId}/price-history/listings`);
export const getReferenceHistory = (cardId: string) => request<PriceReferenceSnapshot[]>(`/api/cards/${cardId}/price-history/references`);
export const captureListingSnapshot = (cardId: string) => request<void>(`/api/cards/${cardId}/price-history/capture-listing-snapshot`, { method: 'POST' });
export const refreshReferencePrices = (cardId: string) => request<void>(`/api/cards/${cardId}/price-history/refresh-reference-prices`, { method: 'POST' });
