import { useMemo, useState } from 'react';
import { AlertsPage } from './pages/AlertsPage';
import { CardDetailPage } from './pages/CardDetailPage';
import { MarketplaceHomePage } from './pages/MarketplaceHomePage';
import { SearchPage } from './pages/SearchPage';
import { SellerInventoryPage } from './pages/SellerInventoryPage';
import { WatchlistPage } from './pages/WatchlistPage';

type Route = { name: 'marketplace' } | { name: 'search' } | { name: 'detail'; cardId: string } | { name: 'inventory' } | { name: 'watchlist' } | { name: 'alerts' };

export function App() {
  const [route, setRoute] = useState<Route>({ name: 'marketplace' });
  const content = useMemo(() => {
    if (route.name === 'marketplace') return <MarketplaceHomePage />;
    if (route.name === 'detail') return <CardDetailPage cardId={route.cardId} />;
    if (route.name === 'inventory') return <SellerInventoryPage />;
    if (route.name === 'watchlist') return <WatchlistPage />;
    if (route.name === 'alerts') return <AlertsPage />;
    return <SearchPage onOpen={(cardId) => setRoute({ name: 'detail', cardId })} />;
  }, [route]);

  return (
    <>
      <nav>
        <button onClick={() => setRoute({ name: 'marketplace' })}>Marketplace</button>
        <button onClick={() => setRoute({ name: 'search' })}>Search</button>
        <button onClick={() => setRoute({ name: 'inventory' })}>Sell</button>
        <button onClick={() => setRoute({ name: 'watchlist' })}>Watchlist</button>
        <button onClick={() => setRoute({ name: 'alerts' })}>Alerts</button>
      </nav>
      {content}
    </>
  );
}
