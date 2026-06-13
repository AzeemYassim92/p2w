import { request } from './http';
import type { WatchlistItem } from '../types/watchlist';

export const getWatchlist = () => request<WatchlistItem[]>('/api/watchlist');
export const addWatchlistItem = (cardId: string, targetPrice?: number, notes?: string) =>
  request<WatchlistItem>('/api/watchlist', { method: 'POST', body: JSON.stringify({ cardId, targetPrice, notes }) });
export const removeWatchlistItem = (watchlistItemId: string) => request<void>(`/api/watchlist/${watchlistItemId}`, { method: 'DELETE' });
