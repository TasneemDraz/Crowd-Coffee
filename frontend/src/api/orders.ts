import type { CreateOrderRequest, OrderDto } from '../types/orders';

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? '/api';

/** Carries the server's ProblemDetails detail message (e.g. "Insufficient stock for Oat Milk:
 * order needs 40, available 20.") so the checkout UI can show something the customer can act on,
 * rather than a generic "something went wrong." */
export class ApiError extends Error {
  readonly status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
  }
}

export async function createOrder(request: CreateOrderRequest): Promise<OrderDto> {
  const response = await fetch(`${API_BASE}/orders`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    const detail = problem?.detail ?? problem?.title ?? `Order creation failed with status ${response.status}.`;
    throw new ApiError(detail, response.status);
  }

  return response.json() as Promise<OrderDto>;
}

/** Backs the checkout return page (Epic 10.2): polls the real, server-confirmed order status
 * after a redirect back from the payment gateway, rather than trusting the redirect's own query
 * parameters. */
export async function getOrder(orderId: string): Promise<OrderDto> {
  const response = await fetch(`${API_BASE}/orders/${encodeURIComponent(orderId)}`);
  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    const detail = problem?.detail ?? problem?.title ?? `Fetching order failed with status ${response.status}.`;
    throw new ApiError(detail, response.status);
  }

  return response.json() as Promise<OrderDto>;
}
