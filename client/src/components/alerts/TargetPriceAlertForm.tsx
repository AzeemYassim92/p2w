import { useState } from 'react';
import { createAlert } from '../../api/alertsApi';

export function TargetPriceAlertForm({ cardId }: { cardId: string }) {
  const [targetPrice, setTargetPrice] = useState('');
  const [status, setStatus] = useState('');

  async function submit() {
    const value = Number(targetPrice);
    if (!Number.isFinite(value) || value <= 0) return;
    await createAlert(cardId, value);
    setStatus('Alert saved');
  }

  return (
    <div className="toolbar compact">
      <input type="number" min="0" step="0.01" value={targetPrice} onChange={(event) => setTargetPrice(event.target.value)} placeholder="Target price" />
      <button onClick={submit}>{status || 'Set Alert'}</button>
    </div>
  );
}
