import { authorizedJson, authorizedJsonBody } from '../lib/adminApi';
import type {
  RecordAdjustmentRequest,
  RecordPurchaseRequest,
  RecordWasteRequest,
  StockItemSummary,
  StockMovement,
} from '../types/adminStock';

export function getStockItems(): Promise<StockItemSummary[]> {
  return authorizedJson<StockItemSummary[]>('/inventory/stock');
}

export function getStockMovements(stockItemId: string): Promise<StockMovement[]> {
  return authorizedJson<StockMovement[]>(`/inventory/stock/${encodeURIComponent(stockItemId)}/movements`);
}

export function recordPurchase(request: RecordPurchaseRequest): Promise<void> {
  return authorizedJsonBody<void>('/inventory/stock/purchase', 'POST', request);
}

export function recordWaste(request: RecordWasteRequest): Promise<void> {
  return authorizedJsonBody<void>('/inventory/stock/waste', 'POST', request);
}

export function recordAdjustment(request: RecordAdjustmentRequest): Promise<void> {
  return authorizedJsonBody<void>('/inventory/stock/adjustment', 'POST', request);
}
