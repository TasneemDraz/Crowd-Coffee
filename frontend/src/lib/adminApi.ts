import { useAuthStore } from '../stores/auth';

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? '/api';

export class ApiError extends Error {
  readonly status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
  }
}

/** Shared by every admin (Epic 13/14) API module — attaches the live access token (auto-
 * refreshing via the auth store if it's near expiry) the same way api/kitchen.ts's
 * authorizedFetch does. Factored out here since four admin modules would otherwise each
 * duplicate this exact wrapper. */
export async function authorizedFetch(path: string, init: RequestInit = {}): Promise<Response> {
  const auth = useAuthStore();
  const token = await auth.getValidAccessToken();
  if (!token) {
    throw new ApiError('Not authenticated.', 401);
  }

  return fetch(`${API_BASE}${path}`, {
    ...init,
    headers: { ...(init.headers ?? {}), Authorization: `Bearer ${token}` },
  });
}

export async function authorizedJson<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await authorizedFetch(path, init);
  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    throw new ApiError(problem?.detail ?? problem?.title ?? `Request failed with status ${response.status}.`, response.status);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export function authorizedJsonBody<T>(path: string, method: 'POST' | 'PUT', body: unknown): Promise<T> {
  return authorizedJson<T>(path, {
    method,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
}
