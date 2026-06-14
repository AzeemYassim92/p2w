import { useEffect, useState } from 'react';
import {
  getCatalogImportRuns,
  previewCatalogImport,
  runCatalogImport,
  type CatalogImportPreview,
  type CatalogImportRun,
  type StartCatalogImportRequest
} from '../api/catalogAdminApi';

export function CatalogImportPage() {
  const [request, setRequest] = useState<StartCatalogImportRequest>({
    sourceName: 'Scryfall',
    gameSlug: 'magic-the-gathering',
    importType: 'Cards',
    dryRun: true,
    maxRecords: 25,
    includeImages: true,
    updateExistingProducts: true,
    createMissingProducts: true,
    useCheckpoint: true,
    saveCheckpoint: true,
    checkpointValue: ''
  });
  const [preview, setPreview] = useState<CatalogImportPreview | null>(null);
  const [run, setRun] = useState<CatalogImportRun | null>(null);
  const [runs, setRuns] = useState<CatalogImportRun[]>([]);
  const [error, setError] = useState('');
  const [isWorking, setIsWorking] = useState(false);

  useEffect(() => {
    void loadRuns(request.sourceName);
  }, [request.sourceName]);

  function update(patch: Partial<StartCatalogImportRequest>) {
    const next = { ...request, ...patch };
    if (patch.sourceName === 'Scryfall') next.gameSlug = 'magic-the-gathering';
    if (patch.sourceName === 'PokemonTCG') next.gameSlug = 'pokemon';
    setRequest(next);
  }

  async function dryRun() {
    setError('');
    setIsWorking(true);
    try {
      setPreview(await previewCatalogImport({ ...request, dryRun: true }));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Preview failed');
    } finally {
      setIsWorking(false);
    }
  }

  async function runImport() {
    setError('');
    setIsWorking(true);
    try {
      const result = await runCatalogImport({ ...request, dryRun: false });
      setRun(result);
      await loadRuns(request.sourceName);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Import failed');
    } finally {
      setIsWorking(false);
    }
  }

  async function loadRuns(sourceName?: string) {
    try {
      setRuns(await getCatalogImportRuns(sourceName));
    } catch {
      setRuns([]);
    }
  }

  return (
    <main>
      <h1>Catalog Import</h1>
      <div className="toolbar import-toolbar">
        <select value={request.sourceName} onChange={(event) => update({ sourceName: event.target.value })}>
          <option>Scryfall</option>
          <option>PokemonTCG</option>
        </select>
        <select value={request.importType} onChange={(event) => update({ importType: event.target.value })}>
          <option>Sets</option>
          <option>Cards</option>
          <option>Full</option>
        </select>
        <input type="number" value={request.maxRecords} min={1} max={5000} onChange={(event) => update({ maxRecords: Number(event.target.value) })} />
        <label className="check-control"><input type="checkbox" checked={request.useCheckpoint} onChange={(event) => update({ useCheckpoint: event.target.checked })} /> Use checkpoint</label>
        <label className="check-control"><input type="checkbox" checked={request.saveCheckpoint} onChange={(event) => update({ saveCheckpoint: event.target.checked })} /> Save next</label>
        <input placeholder="Checkpoint override" value={request.checkpointValue ?? ''} onChange={(event) => update({ checkpointValue: event.target.value })} />
        <button onClick={dryRun} disabled={isWorking}>{isWorking ? 'Working...' : 'Dry Run'}</button>
        <button onClick={runImport} disabled={isWorking}>{isWorking ? 'Working...' : 'Run Import'}</button>
      </div>
      {isWorking && <div className="progress-bar"><span /></div>}
      {error && <p className="error">{error}</p>}
      {preview && (
        <>
          <h2>Preview</h2>
          <p>{preview.externalRecordsRead} read - {preview.wouldCreate} create - {preview.wouldUpdate} update - {preview.wouldSkip} skip</p>
          <CheckpointSummary checkpointValue={preview.checkpointValue} nextCheckpointValue={preview.nextCheckpointValue} hasMore={preview.hasMore} />
          <PreviewTable rows={preview.sampleRows} />
        </>
      )}
      {run && (
        <section className="import-status">
          <h2>Last Result</h2>
          <p>{run.status} - {run.recordsProcessed} processed - {run.recordsCreated} created - {run.recordsUpdated} updated - {run.recordsSkipped} skipped - {run.errorCount} errors</p>
          <CheckpointSummary checkpointValue={run.checkpointValue} nextCheckpointValue={run.nextCheckpointValue} hasMore={run.hasMore} />
        </section>
      )}
      <h2>Recent Runs</h2>
      <ImportRunTable runs={runs} />
    </main>
  );
}

function CheckpointSummary({ checkpointValue, nextCheckpointValue, hasMore }: { checkpointValue?: string; nextCheckpointValue?: string; hasMore: boolean }) {
  return (
    <div className="checkpoint-summary">
      <span>Current: {checkpointValue || 'start'}</span>
      <span>Next: {nextCheckpointValue || 'complete'}</span>
      <span>{hasMore ? 'More pages available' : 'No more pages reported'}</span>
    </div>
  );
}

function PreviewTable({ rows }: { rows: CatalogImportPreview['sampleRows'] }) {
  return (
    <table>
      <thead>
        <tr><th>Action</th><th>Name</th><th>Set</th><th>No.</th><th>Confidence</th><th>Match</th></tr>
      </thead>
      <tbody>
        {rows.map((row) => (
          <tr key={row.externalId}>
            <td>{row.action}</td>
            <td>{row.name}</td>
            <td>{row.setName}</td>
            <td>{row.cardNumber}</td>
            <td>{row.confidenceScore.toFixed(2)}</td>
            <td>{row.matchedCatalogProductName ?? '-'}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function ImportRunTable({ runs }: { runs: CatalogImportRun[] }) {
  return (
    <table>
      <thead>
        <tr><th>Source</th><th>Type</th><th>Status</th><th>Processed</th><th>Created</th><th>Updated</th><th>Skipped</th><th>Errors</th><th>Notes</th></tr>
      </thead>
      <tbody>
        {runs.map((item) => (
          <tr key={item.catalogImportRunId}>
            <td>{item.sourceName}</td>
            <td>{item.importType}</td>
            <td>{item.status}</td>
            <td>{item.recordsProcessed}</td>
            <td>{item.recordsCreated}</td>
            <td>{item.recordsUpdated}</td>
            <td>{item.recordsSkipped}</td>
            <td>{item.errorCount}</td>
            <td>{item.notes ?? '-'}</td>
          </tr>
        ))}
        {runs.length === 0 && <tr><td colSpan={9}>No import runs yet.</td></tr>}
      </tbody>
    </table>
  );
}
