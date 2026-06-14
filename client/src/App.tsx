import { type FormEvent, useMemo, useState } from 'react';
import { AlertsPage } from './pages/AlertsPage';
import { CardDetailPage } from './pages/CardDetailPage';
import { CatalogProductDetailPage } from './pages/CatalogProductDetailPage';
import { MarketplaceHomePage } from './pages/MarketplaceHomePage';
import { CatalogImportPage } from './pages/CatalogImportPage';
import { MappingReviewPage } from './pages/MappingReviewPage';
import { SearchPage } from './pages/SearchPage';
import { SellerInventoryPage } from './pages/SellerInventoryPage';
import { WatchlistPage } from './pages/WatchlistPage';

type Route = { name: 'marketplace' } | { name: 'search' } | { name: 'detail'; cardId: string } | { name: 'product'; productId: string } | { name: 'inventory' } | { name: 'watchlist' } | { name: 'alerts' } | { name: 'import' } | { name: 'mappings' };

export function App() {
  const [route, setRoute] = useState<Route>({ name: 'marketplace' });
  const [country, setCountry] = useState('US');
  const [loggedIn, setLoggedIn] = useState(false);
  const [searchText, setSearchText] = useState('');
  const content = useMemo(() => {
    if (route.name === 'marketplace') return <MarketplaceHomePage onOpenProduct={(productId) => setRoute({ name: 'product', productId })} />;
    if (route.name === 'detail') return <CardDetailPage cardId={route.cardId} />;
    if (route.name === 'product') return <CatalogProductDetailPage productId={route.productId} onBack={() => setRoute({ name: 'marketplace' })} />;
    if (route.name === 'inventory') return <SellerInventoryPage />;
    if (route.name === 'import') return <CatalogImportPage />;
    if (route.name === 'mappings') return <MappingReviewPage />;
    if (route.name === 'watchlist') return <WatchlistPage />;
    if (route.name === 'alerts') return <AlertsPage />;
    return <SearchPage onOpen={(cardId) => setRoute({ name: 'detail', cardId })} />;
  }, [route]);

  function submitSearch(event: FormEvent) {
    event.preventDefault();
    setRoute({ name: 'search' });
  }

  return (
    <>
      <header className="app-header">
        <div className="app-nav">
          <button className="brand-button" onClick={() => setRoute({ name: 'marketplace' })}>
            <span>P2W</span>
            <strong>Collectibles</strong>
          </button>
          <form className="global-search" onSubmit={submitSearch}>
            <input value={searchText} onChange={(event) => setSearchText(event.target.value)} placeholder="Search cards, sealed products, sets..." />
            <button type="submit">Search</button>
          </form>
          <select className="country-select" value={country} onChange={(event) => setCountry(event.target.value)} aria-label="Country">
            <option value="US">US / USD</option>
            <option value="CA">CA / CAD</option>
            <option value="GB">UK / GBP</option>
            <option value="JP">JP / JPY</option>
          </select>
          <button className="utility-button" onClick={() => setRoute({ name: 'watchlist' })}>Wishlist</button>
          <button className="utility-button">Cart 0</button>
          {loggedIn ? (
            <button className="utility-button" onClick={() => setLoggedIn(false)}>Account</button>
          ) : (
            <>
              <button className="utility-button" onClick={() => setLoggedIn(true)}>Login</button>
              <button className="signup-button" onClick={() => setLoggedIn(true)}>Sign Up</button>
            </>
          )}
        </div>
        <nav className="section-nav">
          <button className={route.name === 'marketplace' ? 'active' : ''} onClick={() => setRoute({ name: 'marketplace' })}>Marketplace</button>
          <button className={route.name === 'search' ? 'active' : ''} onClick={() => setRoute({ name: 'search' })}>Browse</button>
          <button className={route.name === 'inventory' ? 'active' : ''} onClick={() => setRoute({ name: 'inventory' })}>Sell</button>
          <button className={route.name === 'import' ? 'active' : ''} onClick={() => setRoute({ name: 'import' })}>Import</button>
          <button className={route.name === 'mappings' ? 'active' : ''} onClick={() => setRoute({ name: 'mappings' })}>Mappings</button>
          <button className={route.name === 'alerts' ? 'active' : ''} onClick={() => setRoute({ name: 'alerts' })}>Alerts</button>
        </nav>
      </header>
      {content}
    </>
  );
}
