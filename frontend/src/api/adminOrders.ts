import { authorizedJson } from '../lib/adminApi';
import type { AdminOrderDetail, AdminOrderSummary, OrderSearchFilters } from '../types/adminOrders';

export function searchOrders(filters: OrderSearchFilters): Promise<AdminOrderSummary[]> {
  const params = new URLSearchParams();
  if (filters.dateFrom) params.set('dateFrom', filters.dateFrom);
  if (filters.dateTo) params.set('dateTo', filters.dateTo);
  if (filters.status) params.set('status', filters.status);
  if (filters.mode) params.set('mode', filters.mode);
  if (filters.paymentMethod) params.set('paymentMethod', filters.paymentMethod);

  const query = params.toString();
  return authorizedJson<AdminOrderSummary[]>(`/admin/orders${query ? `?${query}` : ''}`);
}

export function getOrderDetail(orderId: string): Promise<AdminOrderDetail> {
  return authorizedJson<AdminOrderDetail>(`/admin/orders/${encodeURIComponent(orderId)}`);
}
