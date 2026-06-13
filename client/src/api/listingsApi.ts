import { request } from './http';
import type { Listing } from '../types/listings';

export const getListings = (cardId: string) => request<Listing[]>(`/api/cards/${cardId}/listings`);
export const refreshListings = (cardId: string) => request<void>(`/api/cards/${cardId}/refresh-listings`, { method: 'POST' });
