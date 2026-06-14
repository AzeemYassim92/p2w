import { useEffect, useMemo, useState } from 'react';
import { getCatalogPricingHistory, getCatalogProductDetail, refreshCatalogPricing } from '../api/catalogApi';
import type { CatalogPriceSnapshot, CatalogProductDetail, ProductVariant } from '../types/catalog';
import { money } from '../utils/money';

const conditions = [
  { label: 'Near Mint', short: 'NM', multiplier: 1 },
  { label: 'Lightly Played', short: 'LP', multiplier: 0.82 },
  { label: 'Moderately Played', short: 'MP', multiplier: 0.68 },
  { label: 'Heavily Played', short: 'HP', multiplier: 0.46 },
  { label: 'Damaged', short: 'DMG', multiplier: 0.28 }
];

const chartMultipliers = [0.91, 0.95, 0.93, 0.98, 1.02, 0.99, 1.08, 1.0];

export function CatalogProductDetailPage({ productId, onBack }: { productId: string; onBack: () => void }) {
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
      setProduct(detail);
      setPrices(history);
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
  const highPrice = latestPrice?.highPrice ?? marketPrice * 1.24;
  const variants = product.variants.length > 0 ? product.variants : fallbackVariants(product);
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
            />
            <button className="other-listings-button">View 11 Other Listings <small>As low as {money(lowPrice)}</small></button>
            <div className="seller-links">
              <button>Sell this</button>
              <button>Report a problem</button>
            </div>
          </aside>
        </section>

        <section className="price-content-grid">
          <MarketHistory snapshots={prices} fallbackPrice={marketPrice} />
          <aside className="price-sidebar">
            <ComparisonPrices variants={variants} marketPrice={marketPrice} />
            <PricePoints
              marketPrice={marketPrice}
              listedMedian={listedMedian}
              highPrice={highPrice}
              quantity={126}
              sellers={10}
            />
            <SnapshotPanel marketPrice={marketPrice} />
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
        <dt>Catalog:</dt><dd>{product.externalMappings.length > 0 ? `${product.externalMappings.length} provider mapping${product.externalMappings.length === 1 ? '' : 's'}` : 'Awaiting provider mapping'}</dd>
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

function BuyBox({ condition, product, price, onRefresh, isRefreshing }: {
  condition: string;
  product: CatalogProductDetail;
  price: number;
  onRefresh: () => void;
  isRefreshing: boolean;
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
      <button className="secondary-button refresh-link" onClick={onRefresh} disabled={isRefreshing}>{isRefreshing ? 'Refreshing...' : `Refresh ${product.primarySourceName ?? 'price'}`}</button>
    </section>
  );
}

function MarketHistory({ snapshots, fallbackPrice }: { snapshots: CatalogPriceSnapshot[]; fallbackPrice: number }) {
  const points = snapshots.length > 0 ? snapshots.slice(0, 8).reverse() : demoSnapshots(fallbackPrice);
  const max = Math.max(...points.map((point) => point.marketPrice ?? fallbackPrice), fallbackPrice);
  const min = Math.min(...points.map((point) => point.marketPrice ?? fallbackPrice), fallbackPrice);

  return (
    <section className="market-history-card">
      <div className="price-section-heading">
        <h2>Market Price History</h2>
        <span>Near Mint {money(fallbackPrice)} <b>-6.40%</b></span>
      </div>
      <div className="chart-shell">
        <div className="chart-axis">
          <span>{money(max)}</span>
          <span>{money((max + min) / 2)}</span>
          <span>{money(min)}</span>
        </div>
        <div className="line-chart">
          {points.map((point, index) => {
            const value = point.marketPrice ?? fallbackPrice;
            const pct = max === min ? 50 : ((value - min) / (max - min)) * 100;
            return <span key={`${point.capturedAtUtc}-${index}`} style={{ left: `${index * (100 / Math.max(points.length - 1, 1))}%`, bottom: `${pct}%` }} />;
          })}
        </div>
      </div>
    </section>
  );
}

function ComparisonPrices({ variants, marketPrice }: { variants: ProductVariant[]; marketPrice: number }) {
  const normal = variants.find((variant) => !variant.isFoil && !variant.isReverseHolo) ?? variants[0];
  const premium = variants.find((variant) => variant.isFoil || variant.isReverseHolo) ?? variants[1] ?? variants[0];
  return (
    <section className="side-price-card">
      <h2>Near Mint Comparison Prices</h2>
      <p>Market prices for alternative printings and conditions</p>
      <div className="comparison-row">
        <span>{normal?.variantName ?? 'Normal'}: <strong>{money(marketPrice)}</strong></span>
        <span>{premium?.variantName ?? 'Premium'}: <strong>{money(marketPrice * 1.58)}</strong></span>
      </div>
    </section>
  );
}

function PricePoints({ marketPrice, listedMedian, highPrice, quantity, sellers }: { marketPrice: number; listedMedian: number; highPrice: number; quantity: number; sellers: number }) {
  return (
    <section className="price-points-card">
      <div className="price-points-header">
        <h2>Price Points</h2>
        <span>Near Mint</span>
      </div>
      <div className="price-point-total">
        <div>
          <strong>Market Price</strong>
          <small>Most Recent Sale</small>
        </div>
        <b>{money(marketPrice)}</b>
      </div>
      <div className="volatility-bar">
        <span />
        <i style={{ left: '42%' }} />
        <i style={{ left: '74%' }} />
      </div>
      <dl>
        <div><dt>Listed Median:</dt><dd>{money(listedMedian)}</dd></div>
        <div><dt>Current Quantity:</dt><dd>{quantity}</dd></div>
        <div><dt>Current Sellers:</dt><dd>{sellers}</dd></div>
        <div><dt>High:</dt><dd>{money(highPrice)}</dd></div>
      </dl>
    </section>
  );
}

function SnapshotPanel({ marketPrice }: { marketPrice: number }) {
  return (
    <section className="snapshot-card">
      <div>
        <h2>3 Month Snapshot</h2>
        <button>View More Data</button>
      </div>
      <dl>
        <div><dt>Average Sale</dt><dd>{money(marketPrice * 0.97)}</dd></div>
        <div><dt>Low Sale</dt><dd>{money(marketPrice * 0.72)}</dd></div>
        <div><dt>High Sale</dt><dd>{money(marketPrice * 1.31)}</dd></div>
      </dl>
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

function fallbackVariants(product: CatalogProductDetail): ProductVariant[] {
  return [
    {
      productVariantId: `${product.catalogProductId}-normal`,
      variantName: product.isSealed ? 'Sealed' : 'Normal',
      language: 'EN',
      isFoil: false,
      isReverseHolo: false,
      isFirstEdition: false,
      isPromo: false,
      isSerialized: false,
      isSealedCase: product.isSealed
    },
    {
      productVariantId: `${product.catalogProductId}-foil`,
      variantName: product.isSealed ? 'Case' : 'Foil',
      language: 'EN',
      isFoil: true,
      isReverseHolo: false,
      isFirstEdition: false,
      isPromo: false,
      isSerialized: false,
      isSealedCase: false
    }
  ];
}

function demoSnapshots(price: number): CatalogPriceSnapshot[] {
  return chartMultipliers.map((multiplier, index) => ({
    catalogPriceReferenceSnapshotId: `demo-${index}`,
    catalogProductId: 'demo',
    sourceName: 'Demo',
    marketPrice: price * multiplier,
    lowPrice: price * multiplier * 0.86,
    midPrice: price * multiplier,
    highPrice: price * multiplier * 1.24,
    currency: 'USD',
    capturedAtUtc: new Date(Date.now() - (chartMultipliers.length - index) * 86400000).toISOString()
  }));
}

function detailFallback(product: CatalogProductDetail) {
  const setText = product.setName ? `${product.setName}` : product.categoryName;
  const numberText = product.cardNumber ? ` #${product.cardNumber}` : '';
  const rarityText = product.rarity ? ` ${product.rarity}` : '';
  return `${setText}${numberText}.${rarityText} Catalog details are ready for richer rules text, print data, and provider-specific metadata.`;
}
