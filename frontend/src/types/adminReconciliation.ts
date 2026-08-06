// Mirrors CrowdCoffee.Application.Payments.ReconciliationRowDto (server/src/CrowdCoffee.Application/Payments/ReconciliationDtos.cs).

export interface ReconciliationRow {
  orderId: string;
  orderNumber: string | null;
  orderTotal: number | null;
  orderStatus: string | null;
  gatewayTransactionId: string;
  gatewayAmountEgp: number;
  gatewaySuccessful: boolean;
  receivedAtUtc: string;
}
