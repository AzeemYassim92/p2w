import { request } from './http';

export type StartCatalogImportRequest = {
  sourceName: string;
  gameSlug: string;
  importType: string;
  dryRun: boolean;
  maxRecords: number;
  includeImages: boolean;
  updateExistingProducts: boolean;
  createMissingProducts: boolean;
  useCheckpoint: boolean;
  saveCheckpoint: boolean;
  checkpointValue?: string;
};

export type CatalogImportPreview = {
  sourceName: string;
  gameSlug: string;
  importType: string;
  externalRecordsRead: number;
  existingMatches: number;
  wouldCreate: number;
  wouldUpdate: number;
  wouldSkip: number;
  checkpointValue?: string;
  nextCheckpointValue?: string;
  hasMore: boolean;
  sampleRows: CatalogImportPreviewRow[];
};

export type CatalogImportPreviewRow = {
  externalId: string;
  sourceName: string;
  name: string;
  setName?: string;
  cardNumber?: string;
  rarity?: string;
  action: string;
  confidenceScore: number;
  matchedCatalogProductName?: string;
};

export type CatalogImportRun = {
  catalogImportRunId: string;
  sourceName: string;
  importType: string;
  status: string;
  recordsProcessed: number;
  recordsCreated: number;
  recordsUpdated: number;
  recordsSkipped: number;
  errorCount: number;
  notes?: string;
  checkpointValue?: string;
  nextCheckpointValue?: string;
  hasMore: boolean;
};

export type MappingReview = {
  mappingId: string;
  productName: string;
  setName?: string;
  sourceName: string;
  externalId: string;
  externalUrl?: string;
  confidenceScore?: number;
  mappingStatus: string;
  mappingNotes?: string;
};

export type CatalogPriceSnapshot = {
  catalogPriceReferenceSnapshotId: string;
  catalogProductId: string;
  sourceName: string;
  marketPrice?: number;
  lowPrice?: number;
  midPrice?: number;
  highPrice?: number;
  currency: string;
  capturedAtUtc: string;
};

export function previewCatalogImport(payload: StartCatalogImportRequest) {
  return request<CatalogImportPreview>('/api/catalog/import/preview', { method: 'POST', body: JSON.stringify(payload) });
}

export function runCatalogImport(payload: StartCatalogImportRequest) {
  return request<CatalogImportRun>('/api/catalog/import/run', { method: 'POST', body: JSON.stringify(payload) });
}

export function getCatalogImportRuns(sourceName?: string) {
  const query = sourceName ? `?sourceName=${encodeURIComponent(sourceName)}&take=10` : '?take=10';
  return request<CatalogImportRun[]>(`/api/catalog/import/runs${query}`);
}

export function getMappingsForReview(status = 'NeedsReview') {
  return request<MappingReview[]>(`/api/catalog/mappings/review?status=${encodeURIComponent(status)}&take=50`);
}

export function approveMapping(mappingId: string) {
  return request<MappingReview>(`/api/catalog/mappings/${mappingId}/approve`, { method: 'PATCH' });
}

export function rejectMapping(mappingId: string) {
  return request<MappingReview>(`/api/catalog/mappings/${mappingId}/reject`, { method: 'PATCH' });
}

export function saveMappingNotes(mappingId: string, notes: string) {
  return request<MappingReview>(`/api/catalog/mappings/${mappingId}/notes`, { method: 'PATCH', body: JSON.stringify({ notes }) });
}

export function getCatalogPricingHistory(productId: string) {
  return request<CatalogPriceSnapshot[]>(`/api/catalog/products/${productId}/pricing/history`);
}

export function refreshCatalogPricing(productId: string) {
  return request<void>(`/api/catalog/products/${productId}/pricing/refresh`, { method: 'POST' });
}
