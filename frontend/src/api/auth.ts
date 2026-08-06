import type { AuthResponse } from '../types/auth';

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? '/api';

export class ApiError extends Error {
  readonly status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
  }
}

async function parseAuthResponse(response: Response): Promise<AuthResponse> {
  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    const detail = problem?.detail ?? problem?.title ?? `Request failed with status ${response.status}.`;
    throw new ApiError(detail, response.status);
  }

  return response.json() as Promise<AuthResponse>;
}

export async function login(username: string, password: string): Promise<AuthResponse> {
  const response = await fetch(`${API_BASE}/auth/login`, {
    method: 'POST',
    // The refresh token only ever travels as the httpOnly cookie Story 11.1's login response
    // sets — 'include' is what makes the browser actually store/send it against this API origin.
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password }),
  });

  return parseAuthResponse(response);
}

/** Silent re-auth off the httpOnly refresh cookie alone (no credentials in the request body) —
 * called on app load and whenever an access token expires, so a staff member's session survives
 * a page reload without re-entering a password every time. */
export async function refresh(): Promise<AuthResponse> {
  const response = await fetch(`${API_BASE}/auth/refresh`, { method: 'POST', credentials: 'include' });
  return parseAuthResponse(response);
}

export async function logout(): Promise<void> {
  await fetch(`${API_BASE}/auth/logout`, { method: 'POST', credentials: 'include' });
}
