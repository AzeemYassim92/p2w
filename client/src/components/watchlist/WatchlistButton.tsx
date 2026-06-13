import { useState } from 'react';
import { addWatchlistItem } from '../../api/watchlistApi';

export function WatchlistButton({ cardId }: { cardId: string }) {
  const [status, setStatus] = useState('');

  async function add() {
    await addWatchlistItem(cardId);
    setStatus('Saved');
  }

  return <button onClick={add}>{status || 'Add to Watchlist'}</button>;
}
