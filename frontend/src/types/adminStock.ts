// Mirrors CrowdCoffee.Application.Inventory's admin-facing DTOs (server/src/CrowdCoffee.Application/Inventory/StockAdminDtos.cs).

export interface StockItemSummary {
  id: string;
  name: string;
  unit: 'Gram' | 'Milliliter' | 'Piece';
  quantityOnHand: number;
  quantityReserved: number;
  available: number;
  reorderPoint: number;
  isActive: boolean;
  isLowStock: boolean;
}

export interface StockMovement {
  id: string;
  quantity: number;
  movementType: 'Purchase' | 'Sale' | 'Waste' | 'Adjustment' | 'Return';
  referenceType: string | null;
  referenceId: string | null;
  notes: string | null;
  createdBy: string;
  createdAt: string;
}

export interface RecordPurchaseRequest {
  stockItemId: string;
  quantity: number;
  createdBy: string;
  notes: string | null;
}

export interface RecordWasteRequest {
  stockItemId: string;
  quantity: number;
  createdBy: string;
  reason: string;
}

export interface RecordAdjustmentRequest {
  stockItemId: string;
  quantityDelta: number;
  createdBy: string;
  reason: string;
}
