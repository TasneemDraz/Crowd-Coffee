<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { getReconciliation } from '../api/adminReconciliation';
import { ApiError } from '../lib/adminApi';
import type { ReconciliationRow } from '../types/adminReconciliation';

const rows = ref<ReconciliationRow[]>([]);
const loading = ref(true);
const errorMessage = ref('');

onMounted(async () => {
  try {
    rows.value = await getReconciliation();
  } catch (err) {
    errorMessage.value = err instanceof ApiError ? err.message : 'Failed to load reconciliation data.';
  } finally {
    loading.value = false;
  }
});
</script>

<template>
  <div class="admin-reconciliation">
    <h1>Payment reconciliation</h1>
    <p class="hint">Gateway-confirmed payments matched against orders (Epic 10.6). A blank order column means the gateway reported a transaction for an order id this system never had.</p>

    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
    <p v-if="loading">Loading…</p>

    <table v-else class="admin-table">
      <thead>
        <tr>
          <th>Order #</th><th>Order status</th><th>Order total</th><th>Gateway txn</th><th>Gateway amount</th><th>Success</th><th>Received</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="row in rows" :key="row.gatewayTransactionId" :class="{ mismatch: !row.orderNumber }">
          <td>{{ row.orderNumber ?? '—' }}</td>
          <td>{{ row.orderStatus ?? '—' }}</td>
          <td>{{ row.orderTotal ?? '—' }}</td>
          <td>{{ row.gatewayTransactionId }}</td>
          <td>{{ row.gatewayAmountEgp }} EGP</td>
          <td>{{ row.gatewaySuccessful ? 'Yes' : 'No' }}</td>
          <td>{{ new Date(row.receivedAtUtc).toLocaleString() }}</td>
        </tr>
        <tr v-if="rows.length === 0"><td colspan="7">No payment events yet.</td></tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.admin-reconciliation h1 {
  margin-top: 0;
}

.hint {
  font-size: 13px;
  color: var(--color-ink-faint);
  max-width: 640px;
}

.error {
  color: #b3261e;
}

.admin-table {
  width: 100%;
  border-collapse: collapse;
  background: var(--color-white);
}

.admin-table th,
.admin-table td {
  text-align: left;
  padding: 8px 10px;
  border-bottom: 1px solid var(--color-border-soft);
  font-size: 14px;
}

.mismatch {
  background: oklch(94% 0.06 40);
}
</style>
