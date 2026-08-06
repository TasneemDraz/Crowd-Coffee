// Mirrors CrowdCoffee.Application.Ordering's API-facing DTOs (server/src/CrowdCoffee.Application/Ordering/OrderingDtos.cs).

export type PaymentMethod = 'Cash' | 'Card' | 'Wallet';
export type OrderModeApi = 'Delivery' | 'Pickup';

export interface CreateOrderLineRequest {
  productId: string;
  milkOptionId: string | null;
  shotOptionId: string | null;
  syrupOptionIds: string[];
  quantity: number;
}

export interface CreateOrderRequest {
  customerName: string;
  phone: string;
  mode: OrderModeApi;
  address: string | null;
  latitude: number | null;
  longitude: number | null;
  paymentMethod: PaymentMethod;
  idempotencyKey: string;
  lines: CreateOrderLineRequest[];
  notes: string | null;
}

export interface OrderLineDto {
  productId: string;
  name: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface OrderDto {
  id: string;
  orderNumber: string;
  customerName: string;
  phone: string;
  mode: OrderModeApi;
  address: string | null;
  latitude: number | null;
  longitude: number | null;
  subtotal: number;
  deliveryFee: number;
  total: number;
  paymentMethod: PaymentMethod;
  paymentStatus: string;
  status: string;
  createdAt: string;
  lines: OrderLineDto[];
}
