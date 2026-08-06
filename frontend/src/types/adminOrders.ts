// Mirrors CrowdCoffee.Application.Ordering's admin-facing DTOs (server/src/CrowdCoffee.Application/Ordering/AdminOrderDtos.cs).

export interface AdminOrderSummary {
  id: string;
  orderNumber: string;
  customerName: string;
  mode: string;
  paymentMethod: string;
  status: string;
  total: number;
  createdAt: string;
}

export interface OrderStatusHistoryEntry {
  fromStatus: string;
  toStatus: string;
  changedBy: string;
  changedAt: string;
  reason: string | null;
}

export interface AdminOrderLine {
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  modifiers: string[];
}

export interface AdminOrderDetail {
  id: string;
  orderNumber: string;
  customerName: string;
  phone: string;
  mode: string;
  address: string | null;
  latitude: number | null;
  longitude: number | null;
  subtotal: number;
  deliveryFee: number;
  total: number;
  paymentMethod: string;
  paymentStatus: string;
  status: string;
  createdAt: string;
  confirmedAt: string | null;
  completedAt: string | null;
  notes: string | null;
  lines: AdminOrderLine[];
  statusHistory: OrderStatusHistoryEntry[];
}

export interface OrderSearchFilters {
  dateFrom: string | null; // yyyy-MM-dd
  dateTo: string | null;
  status: string | null;
  mode: string | null;
  paymentMethod: string | null;
}
