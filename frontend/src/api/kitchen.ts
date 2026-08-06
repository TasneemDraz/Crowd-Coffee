import * as signalR from '@microsoft/signalr';
import { useAuthStore } from '../stores/auth';
import type { KitchenOrder } from '../types/kitchen';

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? '/api';

export class ApiError extends Error {
  readonly status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
  }
}

async function authorizedFetch(path: string, init: RequestInit = {}): Promise<Response> {
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

export async function getKitchenBoard(): Promise<KitchenOrder[]> {
  const response = await authorizedFetch('/kitchen/orders');
  if (!response.ok) {
    throw new ApiError(`Failed to load the kitchen board (status ${response.status}).`, response.status);
  }

  return response.json() as Promise<KitchenOrder[]>;
}

export async function advanceOrder(orderId: string): Promise<KitchenOrder> {
  const response = await authorizedFetch(`/kitchen/orders/${encodeURIComponent(orderId)}/advance`, { method: 'POST' });
  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    throw new ApiError(problem?.detail ?? problem?.title ?? `Advance failed (status ${response.status}).`, response.status);
  }

  return response.json() as Promise<KitchenOrder>;
}

/**
 * Story 12.1: connects to OrderHub for live "orderStatusChanged" pushes. accessTokenFactory is
 * called by the SignalR client itself on every (re)connect attempt — including its own
 * automatic-reconnect retries — so it goes through the auth store's transparent-refresh helper
 * rather than capturing one token value up front, which would go stale on a long-lived board.
 */
export function connectOrderHub(onOrderStatusChanged: (order: KitchenOrder) => void): signalR.HubConnection {
  const auth = useAuthStore();
  const connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/orders', {
      accessTokenFactory: async () => (await auth.getValidAccessToken()) ?? '',
    })
    .withAutomaticReconnect()
    .build();

  connection.on('orderStatusChanged', onOrderStatusChanged);
  return connection;
}
