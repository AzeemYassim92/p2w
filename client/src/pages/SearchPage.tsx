import { useEffect, useState } from 'react';
import { getFeaturedProducts, searchCards } from '../api/cardsApi';
import { CardSearchBar } from '../components/cards/CardSearchBar';
import { CardSearchResults } from '../components/cards/CardSearchResults';
import { MarketplaceProductGrid } from '../components/cards/MarketplaceProductGrid';
import { ProductCategoryTabs, type ProductCategory } from '../components/cards/ProductCategoryTabs';
import type { CardSearchResult, MarketplaceProduct } from '../types/cards';

const categories: ProductCategory[] = [
  { id: 'individual-cards', label: 'Individual Cards' },
  { id: 'packs-of-cards', label: 'Packs of Cards' },
  { id: 'boxes-of-cards', label: 'Boxes of Cards' }
];

export function SearchPage({ onOpen }: { onOpen: (cardId: string) => void }) {
  const [query, setQuery] = useState('Charizard');
  const [game, setGame] = useState('All');
  const [activeCategory, setActiveCategory] = useState('individual-cards');
  const [products, setProducts] = useState<MarketplaceProduct[]>([]);
  const [results, setResults] = useState<CardSearchResult[]>([]);
  const [error, setError] = useState('');

  async function loadFeatured(category = activeCategory) {
    setError('');
    try {
      setProducts(await getFeaturedProducts(category, 10));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Featured products failed to load');
    }
  }

  async function runSearch() {
    setError('');
    try {
      setResults(await searchCards(query, game));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Search failed');
    }
  }

  function changeCategory(categoryId: string) {
    setActiveCategory(categoryId);
    setResults([]);
    void loadFeatured(categoryId);
  }

  useEffect(() => {
    void loadFeatured('individual-cards');
  }, []);

  return (
    <main>
      <h1>P2W Cards</h1>
      <ProductCategoryTabs categories={categories} activeCategory={activeCategory} onChange={changeCategory} />
      {products.length > 0 ? (
        <MarketplaceProductGrid products={products} onOpen={onOpen} />
      ) : (
        <section className="empty-panel">
          <h2>{categories.find((category) => category.id === activeCategory)?.label}</h2>
          <p>Marketplace records for this category are next in line.</p>
        </section>
      )}
      <h2>Find a Specific Card</h2>
      <CardSearchBar query={query} game={game} onQueryChange={setQuery} onGameChange={setGame} onSearch={runSearch} />
      {error && <p className="error">{error}</p>}
      <CardSearchResults results={results} onOpen={onOpen} />
    </main>
  );
}
