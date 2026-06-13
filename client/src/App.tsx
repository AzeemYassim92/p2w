import { useMemo, useState } from 'react';
import { AlertsPage } from './pages/AlertsPage';
import { CardDetailPage } from './pages/CardDetailPage';
import { SearchPage } from './pages/SearchPage';
import { WatchlistPage } from './pages/WatchlistPage';

type Route = { name: 'search' } | { name: 'detail'; cardId: string } | { name: 'watchlist' } | { name: 'alerts' };

export function App() {
  const [route, setRoute] = useState<Route>({ name: 'search' });
  const content = useMemo(() => {
    if (route.name === 'detail') return <CardDetailPage cardId={route.cardId} />;
    if (route.name === 'watchlist') return <WatchlistPage />;
    if (route.name === 'alerts') return <AlertsPage />;
    return <SearchPage onOpen={(cardId) => setRoute({ name: 'detail', cardId })} />;
  }, [route]);

  return (
    <>
      <nav>
        <button onClick={() => setRoute({ name: 'search' })}>Marketplace</button>
        <button onClick={() => setRoute({ name: 'watchlist' })}>Watchlist</button>
        <button onClick={() => setRoute({ name: 'alerts' })}>Alerts</button>
      </nav>
      {content}
    </>
  );
}
