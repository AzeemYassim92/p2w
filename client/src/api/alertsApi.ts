import { request } from './http';
import type { PriceAlert } from '../types/alerts';

export const getAlerts = () => request<PriceAlert[]>('/api/alerts');
export const createAlert = (cardId: string, targetPrice: number) =>
  request<PriceAlert>('/api/alerts', { method: 'POST', body: JSON.stringify({ cardId, targetPrice }) });
export const disableAlert = (alertId: string) => request<void>(`/api/alerts/${alertId}/disable`, { method: 'PATCH' });
