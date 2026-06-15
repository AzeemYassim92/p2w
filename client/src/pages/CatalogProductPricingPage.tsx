import { useEffect, useState } from 'react';
import { getCatalogProductDetail } from '../api/catalogApi';
import { ProductMarketIntelligence } from '../components/market/ProductMarketIntelligence';
import type { CatalogProductDetail } from '../types/catalog';

export function CatalogProductPricingPage({
  productId,
  onBack,
  onOpenDetail,
  onOpenSetDashboard
}: {
  productId: string;
  onBack: () => void;
  onOpenDetail: () => void;
  onOpenSetDashboard: () => void;
}) {
  const [product, setProduct] = useState<CatalogProductDetail | null>(null);
  const [error, setError] = useState('');
  const [condition, setCondition] = useState('NearMint');
  const [printing, setPrinting] = useState('All');
  const [language, setLanguage] = useState('English');

  useEffect(() => {
    let cancelled = false;
    async function load() {
      setError('');
      try {
        const detail = await getCatalogProductDetail(productId);
        if (!cancelled) {
          setProduct({
            ...detail,
            variants: Array.isArray(detail.variants) ? detail.variants : [],
            externalMappings: Array.isArray(detail.externalMappings) ? detail.externalMappings : []
          });
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Pricing page failed to load');
        }
      }
    }

    void load();
    return () => { cancelled = true; };
  }, [productId]);

  if (error) {
    return (
      <main className="pricing-page">
        <button className="secondary-button" onClick={onBack}>Back</button>
        <p className="error">{error}</p>
      </main>
    );
  }

  if (!product) {
    return <main className="pricing-page">Loading pricing...</main>;
  }

  return (
    <>
      <section className="listing-filter-bar pricing-filter-bar">
        <button className="filter-pill">All Filters <span>3</span></button>
        <label className="filter-select">
          <span>Condition</span>
          <select value={condition} onChange={(event) => setCondition(event.target.value)}>
            <option value="NearMint">Near Mint</option>
            <option value="LightlyPlayed">Lightly Played</option>
            <option value="ModeratelyPlayed">Moderately Played</option>
            <option value="HeavilyPlayed">Heavily Played</option>
            <option value="Damaged">Damaged</option>
          </select>
        </label>
        <label className="filter-select">
          <span>Printing</span>
          <select value={printing} onChange={(event) => setPrinting(event.target.value)}>
            <option value="All">All Printings</option>
            <option value="Normal">Normal</option>
            <option value="Foil">Foil</option>
            <option value="ReverseHolo">Reverse Holo</option>
          </select>
        </label>
        <label className="filter-select">
          <span>Language</span>
          <select value={language} onChange={(event) => setLanguage(event.target.value)}>
            <option value="English">English</option>
            <option value="Japanese">Japanese</option>
          </select>
        </label>
        <button className="clear-filter" onClick={() => { setCondition('NearMint'); setPrinting('All'); setLanguage('English'); }}>Clear Filters</button>
      </section>

      <main className="pricing-page">
        <nav className="breadcrumb-row" aria-label="Breadcrumbs">
          <button onClick={onBack}>Marketplace</button>
          <span>/</span>
          <button onClick={onOpenSetDashboard}>Sets</button>
          <span>/</span>
          <button>{product.setName ?? product.categoryName}</button>
          <span>/</span>
          <strong>{product.name} Pricing</strong>
        </nav>

        <section className="pricing-hero">
          <div className="pricing-identity">
            {product.imageUrl ? <img src={product.imageUrl} alt="" /> : <div>{product.gameName}</div>}
            <div>
              <p className="eyebrow">Market Pricing</p>
              <h1>{product.name}</h1>
              <p>{product.gameName} / {product.setName ?? product.categoryName}{product.cardNumber ? ` / #${product.cardNumber}` : ''}</p>
              <div className="pricing-hero-actions">
                <button className="secondary-button" onClick={onOpenDetail}>Product Details</button>
                <button className="secondary-button" onClick={onOpenSetDashboard}>Set Market</button>
              </div>
            </div>
          </div>
          <div className="pricing-context-panel">
            <span>Current View</span>
            <strong>{conditionLabel(condition)}</strong>
            <small>{printing === 'All' ? 'All printings' : printing} / {language}</small>
          </div>
        </section>

        <ProductMarketIntelligence productId={product.catalogProductId} />
      </main>
    </>
  );
}

function conditionLabel(condition: string) {
  return condition
    .replace('NearMint', 'Near Mint')
    .replace('LightlyPlayed', 'Lightly Played')
    .replace('ModeratelyPlayed', 'Moderately Played')
    .replace('HeavilyPlayed', 'Heavily Played');
}
