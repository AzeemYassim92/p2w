import { useEffect, useMemo, useState } from 'react';
import { getCatalogPricingHistory, getCatalogProductDetail, refreshCatalogPricing } from '../api/catalogApi';
import type { CatalogPriceSnapshot, CatalogProductDetail } from '../types/catalog';
import { money } from '../utils/money';

const conditions = [
  { label: 'Near Mint', short: 'NM', multiplier: 1 },
  { label: 'Lightly Played', short: 'LP', multiplier: 0.82 },
  { label: 'Moderately Played', short: 'MP', multiplier: 0.68 },
  { label: 'Heavily Played', short: 'HP', multiplier: 0.46 },
  { label: 'Damaged', short: 'DMG', multiplier: 0.28 }
];

export function CatalogProductDetailPage({ productId, onBack, onOpenPricing }: { productId: string; onBack: () => void; onOpenPricing: () => void }) {
  const [product, setProduct] = useState<CatalogProductDetail | null>(null);
  const [prices, setPrices] = useState<CatalogPriceSnapshot[]>([]);
  const [error, setError] = useState('');
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [detailTab, setDetailTab] = useState<'details' | 'legality'>('details');

  async function load() {
    setError('');
    try {
      const [detail, history] = await Promise.all([
        getCatalogProductDetail(productId),
        getCatalogPricingHistory(productId)
      ]);
      setProduct({
        ...detail,
        variants: Array.isArray(detail.variants) ? detail.variants : [],
        externalMappings: Array.isArray(detail.externalMappings) ? detail.externalMappings : []
      });
      setPrices(Array.isArray(history) ? history : []);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Product failed to load');
    }
  }

  async function refreshPrices() {
    setIsRefreshing(true);
    try {
      await refreshCatalogPricing(productId);
      await load();
    } finally {
      setIsRefreshing(false);
    }
  }

  useEffect(() => { void load(); }, [productId]);

  const latestPrice = useMemo(() => prices[0], [prices]);

  if (error) {
    return (
      <main className="tcg-detail-page">
        <button className="secondary-button" onClick={onBack}>Back</button>
        <p className="error">{error}</p>
      </main>
    );
  }

  if (!product) {
    return <main className="tcg-detail-page">Loading...</main>;
  }

  const marketPrice = latestPrice?.marketPrice ?? product.estimatedMarketPrice ?? 0;
  const listedMedian = latestPrice?.midPrice ?? marketPrice * 0.98;
  const lowPrice = latestPrice?.lowPrice ?? marketPrice * 0.86;
  const primaryCondition = conditions[0];

  return (
    <>
      <section className="listing-filter-bar">
        <button className="filter-pill">All Filters <span>1</span></button>
        <button className="filter-pill">Condition</button>
        <button className="filter-pill">Printing</button>
        <button className="filter-pill active">English</button>
        <button className="clear-filter">Clear Filters</button>
      </section>

      <main className="tcg-detail-page">
        <nav className="breadcrumb-row" aria-label="Breadcrumbs">
          <button onClick={onBack}>All Categories</button>
          <span>/</span>
          <button>{product.gameName} Cards</button>
          <span>/</span>
          <button>{product.setName ?? product.categoryName}</button>
          <span>/</span>
          <strong>{product.name}</strong>
        </nav>

        <section className="tcg-product-grid">
          <ProductImage product={product} />

          <div className="tcg-main-column">
            <header className="tcg-title-block">
              <h1>{product.name}{product.setCode ? ` - ${product.setName} (${product.setCode})` : ''}</h1>
              <p>{product.setName ?? product.categoryName}</p>
            </header>

            {product.releaseDate && new Date(product.releaseDate) > new Date() && (
              <aside className="presale-alert">
                <strong>Presale Item:</strong> This product has an estimated release date of {new Date(product.releaseDate).toLocaleDateString()}. Product details may change until release information is finalized.
              </aside>
            )}

            <section className="tcg-info-card">
              <div className="mini-tabs">
                <button className={detailTab === 'details' ? 'active' : ''} onClick={() => setDetailTab('details')}>Product Details</button>
                <button className={detailTab === 'legality' ? 'active' : ''} onClick={() => setDetailTab('legality')}>Legality</button>
              </div>

              {detailTab === 'details' ? <ProductDetails product={product} /> : <LegalityPanel product={product} />}
            </section>
          </div>

          <aside className="buy-column">
            <BuyBox
              condition={primaryCondition.label}
              product={product}
              price={listedMedian}
              onRefresh={() => void refreshPrices()}
              isRefreshing={isRefreshing}
              onOpenPricing={onOpenPricing}
            />
            <button className="other-listings-button" onClick={onOpenPricing}>View Market Pricing <small>As low as {money(lowPrice)}</small></button>
            <div className="seller-links">
              <button>Sell this</button>
              <button>Report a problem</button>
            </div>
          </aside>
        </section>
      </main>
    </>
  );
}

function ProductDetails({ product }: { product: CatalogProductDetail }) {
  return (
    <div className="product-copy">
      <p>{product.description ?? detailFallback(product)}</p>
      <dl>
        {product.rarity && <><dt>Rarity:</dt><dd>{product.rarity}</dd></>}
        {product.cardNumber && <><dt>#:</dt><dd>{product.cardNumber}</dd></>}
        <dt>Card Type:</dt><dd>{product.productType}</dd>
        {product.artist && <><dt>Artist:</dt><dd>{product.artist}</dd></>}
        <dt>Catalog:</dt><dd>{providerMappingText(product.externalMappings)}</dd>
      </dl>
    </div>
  );
}

function LegalityPanel({ product }: { product: CatalogProductDetail }) {
  const legal = product.isSingleCard ? 'Format legality will come from game-specific rules providers.' : 'Sealed products do not use format legality.';
  return (
    <div className="product-copy">
      <p>{legal}</p>
      <dl>
        <dt>Standard:</dt><dd>Pending</dd>
        <dt>Expanded:</dt><dd>Pending</dd>
        <dt>Commander:</dt><dd>Pending</dd>
      </dl>
    </div>
  );
}

function providerMappingText(mappings: CatalogProductDetail['externalMappings'] | undefined) {
  const count = mappings?.length ?? 0;
  return count > 0 ? `${count} provider mapping${count === 1 ? '' : 's'}` : 'Awaiting provider mapping';
}

function BuyBox({ condition, product, price, onRefresh, isRefreshing, onOpenPricing }: {
  condition: string;
  product: CatalogProductDetail;
  price: number;
  onRefresh: () => void;
  isRefreshing: boolean;
  onOpenPricing: () => void;
}) {
  return (
    <section className="buy-box">
      <p>{condition}</p>
      <strong>{money(price)}</strong>
      <small>shipping: included</small>
      <p className="seller-line">Sold by <a href="#">P2W verified seller</a></p>
      <div className="cart-row">
        <select defaultValue="1"><option>1</option><option>2</option><option>3</option></select>
        <span>of 5</span>
        <button>Add to Cart</button>
      </div>
      <p className="financing-line">Pay in 4 interest-free payments on purchases of {money(price)} or more.</p>
      <button className="secondary-button refresh-link" onClick={onOpenPricing}>Open Market Pricing</button>
      <button className="secondary-button refresh-link" onClick={onRefresh} disabled={isRefreshing}>{isRefreshing ? 'Refreshing...' : `Refresh ${product.primarySourceName ?? 'price'}`}</button>
    </section>
  );
}

function ProductImage({ product }: { product: CatalogProductDetail }) {
  const [failed, setFailed] = useState(false);
  return (
    <figure className="tcg-image-panel">
      {!failed && product.imageUrl ? (
        <img src={product.imageUrl} alt={product.name} onError={() => setFailed(true)} />
      ) : (
        <div>{product.gameName}</div>
      )}
    </figure>
  );
}

function detailFallback(product: CatalogProductDetail) {
  const setText = product.setName ? `${product.setName}` : product.categoryName;
  const numberText = product.cardNumber ? ` #${product.cardNumber}` : '';
  const rarityText = product.rarity ? ` ${product.rarity}` : '';
  return `${setText}${numberText}.${rarityText} Catalog details are ready for richer rules text, print data, and provider-specific metadata.`;
}
