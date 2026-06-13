import { useEffect, useState } from 'react';
import { disableAlert, getAlerts } from '../api/alertsApi';
import type { PriceAlert } from '../types/alerts';
import { money } from '../utils/money';

export function AlertsPage() {
  const [alerts, setAlerts] = useState<PriceAlert[]>([]);
  async function load() { setAlerts(await getAlerts()); }
  async function disable(id: string) { await disableAlert(id); await load(); }
  useEffect(() => { void load(); }, []);
  return (
    <main>
      <h1>Alerts</h1>
      <table>
        <thead><tr><th>Card</th><th>Target</th><th>Status</th><th></th></tr></thead>
        <tbody>
          {alerts.map((alert) => (
            <tr key={alert.priceAlertId}>
              <td>{alert.cardName}</td>
              <td>{money(alert.targetPrice)}</td>
              <td>{alert.hasTriggered ? 'Triggered' : alert.isActive ? 'Active' : 'Disabled'}</td>
              <td>{alert.isActive && <button onClick={() => disable(alert.priceAlertId)}>Disable</button>}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </main>
  );
}
