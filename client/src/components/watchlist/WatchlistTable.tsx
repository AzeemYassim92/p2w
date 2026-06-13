import type { WatchlistItem } from '../../types/watchlist';
import { money } from '../../utils/money';

type Props = { items: WatchlistItem[]; onRemove: (id: string) => void };

export function WatchlistTable({ items, onRemove }: Props) {
  return (
    <table>
      <thead><tr><th>Card</th><th>Game</th><th>Current</th><th>Target</th><th></th></tr></thead>
      <tbody>
        {items.map((item) => (
          <tr key={item.watchlistItemId}>
            <td>{item.cardName}</td>
            <td>{item.game}</td>
            <td>{money(item.currentLowestPrice)}</td>
            <td>{money(item.targetPrice)}</td>
            <td><button onClick={() => onRemove(item.watchlistItemId)}>Remove</button></td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
