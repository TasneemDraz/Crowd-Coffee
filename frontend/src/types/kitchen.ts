// Mirrors CrowdCoffee.Application.Kitchen's KitchenOrderDto (server/src/CrowdCoffee.Application/Kitchen/KitchenDtos.cs).

export type KitchenOrderMode = 'Delivery' | 'Pickup';

export interface KitchenOrderLine {
  productName: string;
  quantity: number;
  modifiers: string[];
}

export interface KitchenOrder {
  id: string;
  orderNumber: string;
  mode: KitchenOrderMode;
  address: string | null;
  latitude: number | null;
  longitude: number | null;
  customerName: string;
  phone: string;
  status: string;
  createdAt: string;
  confirmedAt: string | null;
  notes: string | null;
  lines: KitchenOrderLine[];
}
