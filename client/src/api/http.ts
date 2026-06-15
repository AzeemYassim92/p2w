const baseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://127.0.0.1:5087';

export async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, {
    headers: { 'Content-Type': 'application/json', ...(init?.headers ?? {}) },
    ...init
  });

  if (!response.ok) {
    const body = await response.json().catch(() => ({ error: response.statusText }));
    throw new Error(body.error ?? response.statusText);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const json = await response.json();
  return normalizeResponse(json) as T;
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
