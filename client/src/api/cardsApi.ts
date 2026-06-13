import { request } from './http';
import type { CardDetail, CardSearchResult, MarketplaceProduct } from '../types/cards';

export const searchCards = (query: string, game: string) =>
  request<CardSearchResult[]>(`/api/cards/search?query=${encodeURIComponent(query)}&game=${encodeURIComponent(game)}`);

export const getCardDetail = (cardId: string) => request<CardDetail>(`/api/cards/${cardId}`);

export const getFeaturedProducts = (productType = 'individual-cards', take = 10) =>
  request<MarketplaceProduct[]>(`/api/cards/featured?productType=${encodeURIComponent(productType)}&take=${take}`);
