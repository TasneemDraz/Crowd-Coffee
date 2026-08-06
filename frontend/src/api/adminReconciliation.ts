import { authorizedJson } from '../lib/adminApi';
import type { ReconciliationRow } from '../types/adminReconciliation';

export function getReconciliation(): Promise<ReconciliationRow[]> {
  return authorizedJson<ReconciliationRow[]>('/payments/reconciliation');
}
