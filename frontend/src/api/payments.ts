const API_BASE = import.meta.env.VITE_API_BASE_URL ?? '/api';

export interface InitiatePaymentResponse {
  redirectUrl: string;
}

/**
 * Epic 10.2: hosted checkout. The API never hands back anything card-shaped — just a URL to a
 * gateway-hosted payment page (PCI scope stays at zero, §9). Thrown as a plain Error (not
 * ApiError) since the one thing the checkout page does with a failure here is show it — no
 * status-code branching needed.
 */
export async function initiatePayment(orderId: string): Promise<InitiatePaymentResponse> {
  const response = await fetch(`${API_BASE}/payments/initiate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ orderId }),
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    throw new Error(problem?.detail ?? problem?.title ?? `Payment initiation failed with status ${response.status}.`);
  }

  return response.json() as Promise<InitiatePaymentResponse>;
}
