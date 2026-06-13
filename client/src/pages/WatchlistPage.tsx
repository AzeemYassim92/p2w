import { useEffect, useState } from 'react';
import { getWatchlist, removeWatchlistItem } from '../api/watchlistApi';
import { WatchlistTable } from '../components/watchlist/WatchlistTable';
import type { WatchlistItem } from '../types/watchlist';

export function WatchlistPage() {
  const [items, setItems] = useState<WatchlistItem[]>([]);
  async function load() { setItems(await getWatchlist()); }
  async function remove(id: string) { await removeWatchlistItem(id); await load(); }
  useEffect(() => { void load(); }, []);
  return <main><h1>Watchlist</h1><WatchlistTable items={items} onRemove={remove} /></main>;
}
