import type { CardSearchResult } from '../../types/cards';
import { money } from '../../utils/money';

type Props = {
  results: CardSearchResult[];
  onOpen: (cardId: string) => void;
};

export function CardSearchResults({ results, onOpen }: Props) {
  return (
    <div className="grid">
      {results.map((card) => (
        <button className="result-card" key={card.cardId} onClick={() => onOpen(card.cardId)}>
          <img src={card.imageUrl} alt="" />
          <strong>{card.name}</strong>
          <span>{card.game} · {card.setName}</span>
          <span>{card.rarity ?? 'Unknown rarity'} · {card.cardNumber ?? '-'}</span>
          <span>Lowest {money(card.lowestListingPrice)}</span>
        </button>
      ))}
    </div>
  );
}
