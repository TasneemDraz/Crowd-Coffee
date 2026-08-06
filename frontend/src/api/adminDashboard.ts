import { authorizedJson } from '../lib/adminApi';
import type { DailyDashboard } from '../types/adminDashboard';

export function getDailyDashboard(): Promise<DailyDashboard> {
  return authorizedJson<DailyDashboard>('/admin/dashboard/today');
}
