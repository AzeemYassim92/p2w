export type WatchlistItem = {
  watchlistItemId: string;
  cardId: string;
  cardName: string;
  game: string;
  setName: string;
  imageUrl?: string;
  targetPrice?: number;
  currentLowestPrice?: number;
  notes?: string;
  createdUtc: string;
};
