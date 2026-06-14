import { useEffect, useState } from 'react';
import { approveMapping, getMappingsForReview, rejectMapping, saveMappingNotes, type MappingReview } from '../api/catalogAdminApi';

export function MappingReviewPage() {
  const [mappings, setMappings] = useState<MappingReview[]>([]);
  const [error, setError] = useState('');

  async function load() {
    setError('');
    try {
      setMappings(await getMappingsForReview('NeedsReview'));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Mappings failed to load');
    }
  }

  useEffect(() => { void load(); }, []);

  async function mutate(action: () => Promise<MappingReview>) {
    try {
      await action();
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Mapping update failed');
    }
  }

  return (
    <main>
      <h1>Mapping Review</h1>
      {error && <p className="error">{error}</p>}
      <table>
        <thead>
          <tr><th>Product</th><th>Set</th><th>Source</th><th>External</th><th>Confidence</th><th>Notes</th><th></th></tr>
        </thead>
        <tbody>
          {mappings.map((mapping) => (
            <tr key={mapping.mappingId}>
              <td>{mapping.productName}</td>
              <td>{mapping.setName}</td>
              <td>{mapping.sourceName}</td>
              <td>{mapping.externalId}</td>
              <td>{mapping.confidenceScore?.toFixed(2)}</td>
              <td>
                <input defaultValue={mapping.mappingNotes ?? ''} onBlur={(event) => void mutate(() => saveMappingNotes(mapping.mappingId, event.target.value))} />
              </td>
              <td className="row-actions">
                <button onClick={() => void mutate(() => approveMapping(mapping.mappingId))}>Approve</button>
                <button className="secondary-button" onClick={() => void mutate(() => rejectMapping(mapping.mappingId))}>Reject</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </main>
  );
}
