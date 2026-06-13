import { useEffect, useMemo, useState } from 'react';
import { getMarketplaceHome } from '../api/catalogApi';
import type { CatalogProduct, CardSet, Game, MarketplaceHome, ProductCategory, ProviderCapability } from '../types/catalog';
import { money } from '../utils/money';

export function MarketplaceHomePage() {
  const [home, setHome] = useState<MarketplaceHome | null>(null);
  const [activeGame, setActiveGame] = useState<string>('');
  const [error, setError] = useState('');

  useEffect(() => {
    setError('');
    void getMarketplaceHome(activeGame || undefined)
      .then(setHome)
      .catch((err) => setError(err instanceof Error ? err.message : 'Marketplace failed to load'));
  }, [activeGame]);

  const categories = useMemo(() => home?.categories.flatMap((category) => [category, ...category.children]) ?? [], [home]);

  return (
    <main className="marketplace-home">
      <section className="marketplace-header">
        <div>
          <p className="eyebrow">Trading card catalog</p>
          <h1>P2W Cards Marketplace</h1>
          <p>Browse singles, packs, boxes, and upcoming sets across the games we are prioritizing first.</p>
        </div>
        <div className="marketplace-actions">
          <button>Sell Inventory</button>
          <button className="secondary-button">Price Alerts</button>
        </div>
      </section>

      {home && <GameTabs games={home.primaryGames} activeGame={activeGame} onChange={setActiveGame} />}
      {error && <p className="error">{error}</p>}

      {home && (
        <>
          <CategoryGrid categories={categories} />
          <ProductSection title="Trending Now" products={home.trendingProducts} />
          <ProductSection title="Featured Products" products={home.featuredProducts} />
          <SetSection title="Latest Sets" sets={home.latestSets} />
          <SetSection title="Upcoming Sets" sets={home.upcomingSets} />
          <ProviderCapabilityPanel providers={home.providerCapabilities} />
        </>
      )}
    </main>
  );
}

function GameTabs({ games, activeGame, onChange }: { games: Game[]; activeGame: string; onChange: (slug: string) => void }) {
  return (
    <div className="game-tabs" aria-label="Games">
      <button className={!activeGame ? 'active' : ''} onClick={() => onChange('')}>
        All
      </button>
      {games.map((game) => (
        <button key={game.gameId} className={activeGame === game.slug ? 'active' : ''} onClick={() => onChange(game.slug)}>
          {game.name}
        </button>
      ))}
    </div>
  );
}

function CategoryGrid({ categories }: { categories: ProductCategory[] }) {
  return (
    <section>
      <h2>Shop by Category</h2>
      <div className="category-grid">
        {categories.slice(0, 12).map((category) => (
          <button key={category.productCategoryId} className="category-tile">
            <span>{category.name}</span>
            <small>{category.description}</small>
          </button>
        ))}
      </div>
    </section>
  );
}

function ProductSection({ title, products }: { title: string; products: CatalogProduct[] }) {
  if (products.length === 0) {
    return null;
  }

  return (
    <section>
      <h2>{title}</h2>
      <div className="market-grid">
        {products.map((product) => (
          <article className="market-product" key={product.catalogProductId}>
            <CatalogProductImage product={product} />
            <div>
              <p className="product-type">{product.gameName} / {product.categoryName}</p>
              <h3>{product.name}</h3>
              <p>{product.setName ?? 'No set'}{product.setCode ? ` · ${product.setCode}` : ''}</p>
              <p>{product.rarity ?? product.productType}{product.cardNumber ? ` · ${product.cardNumber}` : ''}</p>
            </div>
            <div className="price-row">
              <strong>{money(product.estimatedMarketPrice ?? 0)}</strong>
              {product.primarySourceUrl && (
                <a href={product.primarySourceUrl} target="_blank" rel="noreferrer">
                  {product.primarySourceName}
                </a>
              )}
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}

function CatalogProductImage({ product }: { product: CatalogProduct }) {
  const [failed, setFailed] = useState(false);
  const [backFailed, setBackFailed] = useState(false);
  const shouldUseBack = failed || !product.imageUrl || product.imageUrl.includes('placehold.co');

  if (shouldUseBack) {
    const backUrl = cardBackUrl(product.gameName);
    return (
      <div className="catalog-image-wrap">
        {backUrl && !backFailed ? (
          <img className="official-card-back" src={backUrl} alt={`${cardBackLabel(product.gameName)} card back`} loading="lazy" onError={() => setBackFailed(true)} />
        ) : (
          <div className={`card-back ${cardBackClass(product.gameName)}`}>
            <span>{cardBackLabel(product.gameName)}</span>
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="catalog-image-wrap">
      <img src={product.imageUrl} alt={product.name} loading="lazy" onError={() => setFailed(true)} />
    </div>
  );
}

function cardBackClass(gameName: string) {
  const normalized = gameName.toLowerCase();
  if (normalized.includes('pokemon')) return 'pokemon-back';
  if (normalized.includes('one piece')) return 'one-piece-back';
  return 'magic-back';
}

function cardBackLabel(gameName: string) {
  const normalized = gameName.toLowerCase();
  if (normalized.includes('pokemon')) return 'Pokemon';
  if (normalized.includes('one piece')) return 'One Piece';
  return 'Magic';
}

function cardBackUrl(gameName: string) {
  const normalized = gameName.toLowerCase();
  if (normalized.includes('pokemon')) {
    return 'https://upload.wikimedia.org/wikipedia/en/thumb/3/3b/Pokemon_Trading_Card_Game_cardback.jpg/250px-Pokemon_Trading_Card_Game_cardback.jpg';
  }
  if (normalized.includes('one piece')) {
    return 'https://upload.wikimedia.org/wikipedia/en/3/32/One_Piece_Card_Game_back.webp';
  }
  if (normalized.includes('magic')) {
    return 'https://upload.wikimedia.org/wikipedia/en/thumb/a/aa/Magic_the_gathering-card_back.jpg/250px-Magic_the_gathering-card_back.jpg';
  }
  return '';
}

function SetSection({ title, sets }: { title: string; sets: CardSet[] }) {
  if (sets.length === 0) {
    return null;
  }

  return (
    <section>
      <h2>{title}</h2>
      <div className="set-grid">
        {sets.map((set) => (
          <article className="set-card" key={set.cardSetId}>
            <img src={set.symbolUrl} alt="" />
            <div>
              <strong>{set.name}</strong>
              <p>{set.gameName} · {set.code ?? 'TBD'}</p>
              <p>{set.releaseDate ? new Date(set.releaseDate).toLocaleDateString() : 'Release TBD'}</p>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}

function ProviderCapabilityPanel({ providers }: { providers: ProviderCapability[] }) {
  return (
    <section>
      <h2>Provider Readiness</h2>
      <div className="provider-grid">
        {providers.map((provider) => (
          <article className="provider-card" key={provider.sourceName}>
            <div className="provider-title">
              <strong>{provider.sourceName}</strong>
              <span className={provider.isConfigured ? 'status-on' : 'status-off'}>{provider.isConfigured ? 'Configured' : 'Planned'}</span>
            </div>
            <p>{provider.notes}</p>
            <small>
              {provider.supportsCatalogSearch ? 'Catalog' : ''}
              {provider.supportsMarketplaceListings ? ' Listings' : ''}
              {provider.supportsPriceReference ? ' Prices' : ''}
            </small>
          </article>
        ))}
      </div>
    </section>
  );
}
