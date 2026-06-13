import type { CardDetail } from '../../types/cards';

export function CardDetailHeader({ card }: { card: CardDetail }) {
  return (
    <section className="detail-header">
      <img src={card.imageUrl} alt="" />
      <div>
        <h1>{card.name}</h1>
        <p>{card.game} · {card.setName}</p>
        <p>{card.rarity ?? 'Unknown rarity'} · {card.cardNumber ?? '-'}</p>
        <p>{card.artist ?? 'Unknown artist'}</p>
      </div>
    </section>
  );
}
