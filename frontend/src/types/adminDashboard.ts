// Mirrors CrowdCoffee.Application.Dashboard's DailyDashboardDto (server/src/CrowdCoffee.Application/Dashboard/DashboardDtos.cs).

export interface TopSellingItem {
  productName: string;
  quantitySold: number;
}

export interface DailyDashboard {
  date: string;
  revenue: number;
  orderCount: number;
  averageOrderValue: number;
  deliveryCount: number;
  pickupCount: number;
  topSellingItems: TopSellingItem[];
  lowStockCount: number;
}
