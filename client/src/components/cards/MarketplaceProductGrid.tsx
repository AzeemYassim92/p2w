import type { MarketplaceProduct } from '../../types/cards';
import { money } from '../../utils/money';

type Props = {
  products: MarketplaceProduct[];
  onOpen: (cardId: string) => void;
};

export function MarketplaceProductGrid({ products, onOpen }: Props) {
  return (
    <div className="market-grid">
      {products.map((product) => (
        <article className="market-product" key={product.cardId}>
          <button className="image-button" onClick={() => onOpen(product.cardId)}>
            <img src={product.imageUrl} alt="" />
          </button>
          <div>
            <p className="product-type">{product.productType}</p>
            <h3>{product.name}</h3>
            <p>{product.game} · {product.setName}</p>
            <p>{product.rarity ?? 'Unknown rarity'} · {product.cardNumber ?? '-'}</p>
          </div>
          <div className="price-row">
            <strong>{money(product.price, product.currency)}</strong>
            <a href={product.sourceUrl} target="_blank" rel="noreferrer">{product.sourceName}</a>
          </div>
        </article>
      ))}
    </div>
  );
}
