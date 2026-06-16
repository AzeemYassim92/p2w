const baseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://127.0.0.1:5087';

export function logClientEvent(eventName: string, message?: string, data?: Record<string, unknown>, level = 'Information') {
  try {
    void fetch(`${baseUrl}/api/diagnostics/client-log`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ level, eventName, message, data })
    }).catch(() => undefined);
  } catch {
    // Local diagnostics should never break the UI.
  }
}

export async function checkApiHealth(): Promise<void> {
  const response = await fetch(`${baseUrl}/api/diagnostics/health`);
  if (!response.ok) {
    throw new Error(response.statusText);
  }
}

export async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const method = init?.method ?? 'GET';
  const started = performance.now();
  logClientEvent('frontend.api.request', 'Frontend API request started.', { method, path });

  try {
    const response = await fetch(`${baseUrl}${path}`, {
      headers: { 'Content-Type': 'application/json', ...(init?.headers ?? {}) },
      ...init
    });

    const elapsedMs = Math.round(performance.now() - started);
    logClientEvent('frontend.api.response', 'Frontend API response received.', { method, path, status: response.status, elapsedMs });

    if (!response.ok) {
      const body = await response.json().catch(() => ({ error: response.statusText }));
      logClientEvent('frontend.api.error', 'Frontend API request returned an error status.', { method, path, status: response.status, body }, 'Warning');
      throw new Error(body.error ?? response.statusText);
    }

    if (response.status === 204) {
      return undefined as T;
    }

    const json = await response.json();
    return normalizeResponse(json) as T;
  } catch (error) {
    logClientEvent('frontend.api.failed', 'Frontend API request failed before completion.', {
      method,
      path,
      elapsedMs: Math.round(performance.now() - started),
      error: error instanceof Error ? error.message : String(error)
    }, 'Error');
    throw error;
  }
}

function normalizeResponse(value: unknown) {
  if (
    value &&
    typeof value === 'object' &&
    !Array.isArray(value) &&
    'value' in value &&
    Array.isArray((value as { value?: unknown }).value)
  ) {
    return (value as { value: unknown[] }).value;
  }

  return value;
}
