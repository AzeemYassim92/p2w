import { useEffect, useState } from 'react';
import { getSellerInventory } from '../api/catalogApi';
import type { SellerInventoryItem } from '../types/catalog';
import { money } from '../utils/money';

export function SellerInventoryPage() {
  const [items, setItems] = useState<SellerInventoryItem[]>([]);
  const [error, setError] = useState('');

  useEffect(() => {
    void getSellerInventory()
      .then(setItems)
      .catch((err) => setError(err instanceof Error ? err.message : 'Inventory failed to load'));
  }, []);

  return (
    <main>
      <h1>Seller Inventory</h1>
      {error && <p className="error">{error}</p>}
      <section className="empty-panel">
        <h2>Draft Listings</h2>
        <p>No seller inventory has been added yet.</p>
      </section>
      <table>
        <thead>
          <tr>
            <th>Product</th>
            <th>Game</th>
            <th>Quantity</th>
            <th>Ask</th>
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <tr key={item.sellerInventoryItemId}>
              <td>{item.productName}</td>
              <td>{item.gameName}</td>
              <td>{item.quantity}</td>
              <td>{item.askingPrice ? money(item.askingPrice) : 'Unset'}</td>
              <td>{item.isAvailableForSale ? 'For sale' : 'Draft'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </main>
  );
}
