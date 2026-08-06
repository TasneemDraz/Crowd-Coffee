<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { getStockItems, getStockMovements, recordAdjustment, recordPurchase, recordWaste } from '../api/adminStock';
import { ApiError } from '../lib/adminApi';
import { useAuthStore } from '../stores/auth';
import type { StockItemSummary, StockMovement } from '../types/adminStock';

const auth = useAuthStore();

const items = ref<StockItemSummary[]>([]);
const loading = ref(true);
const errorMessage = ref('');

const expandedItemId = ref<string | null>(null);
const movements = ref<StockMovement[]>([]);
const loadingMovements = ref(false);

type MovementKind = 'purchase' | 'waste' | 'adjustment';
const form = ref({ stockItemId: '', kind: 'purchase' as MovementKind, quantity: 0, reason: '' });
const submitting = ref(false);

async function loadItems() {
  loading.value = true;
  errorMessage.value = '';
  try {
    items.value = await getStockItems();
  } catch (err) {
    errorMessage.value = err instanceof ApiError ? err.message : 'Failed to load stock items.';
  } finally {
    loading.value = false;
  }
}

onMounted(loadItems);

async function toggleLedger(itemId: string) {
  if (expandedItemId.value === itemId) {
    expandedItemId.value = null;
    return;
  }

  expandedItemId.value = itemId;
  loadingMovements.value = true;
  try {
    movements.value = await getStockMovements(itemId);
  } catch (err) {
    errorMessage.value = err instanceof ApiError ? err.message : 'Failed to load the movement ledger.';
  } finally {
    loadingMovements.value = false;
  }
}

async function submitMovement() {
  errorMessage.value = '';
  if (!form.value.stockItemId || form.value.quantity <= 0) {
    errorMessage.value = 'Select an item and enter a positive quantity.';
    return;
  }
  if (form.value.kind !== 'purchase' && !form.value.reason.trim()) {
    errorMessage.value = 'A reason is required for waste and adjustments.';
    return;
  }

  submitting.value = true;
  try {
    const createdBy = auth.username ?? 'admin';
    if (form.value.kind === 'purchase') {
      await recordPurchase({ stockItemId: form.value.stockItemId, quantity: form.value.quantity, createdBy, notes: form.value.reason || null });
    } else if (form.value.kind === 'waste') {
      await recordWaste({ stockItemId: form.value.stockItemId, quantity: form.value.quantity, createdBy, reason: form.value.reason });
    } else {
      await recordAdjustment({ stockItemId: form.value.stockItemId, quantityDelta: form.value.quantity, createdBy, reason: form.value.reason });
    }

    await loadItems();
    if (expandedItemId.value === form.value.stockItemId) {
      movements.value = await getStockMovements(form.value.stockItemId);
    }
    form.value = { stockItemId: form.value.stockItemId, kind: form.value.kind, quantity: 0, reason: '' };
  } catch (err) {
    errorMessage.value = err instanceof ApiError ? err.message : 'Failed to record the movement.';
  } finally {
    submitting.value = false;
  }
}
</script>

<template>
  <div class="admin-stock">
    <h1>Stock management</h1>

    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>

    <form class="movement-form" @submit.prevent="submitMovement">
      <select v-model="form.stockItemId" required>
        <option value="" disabled>Stock item</option>
        <option v-for="item in items" :key="item.id" :value="item.id">{{ item.name }}</option>
      </select>
      <select v-model="form.kind">
        <option value="purchase">Purchase (+)</option>
        <option value="waste">Waste (-)</option>
        <option value="adjustment">Adjustment (±)</option>
      </select>
      <input v-model.number="form.quantity" type="number" step="0.01" :placeholder="form.kind === 'adjustment' ? 'Delta (+/-)' : 'Quantity'" required />
      <input v-model="form.reason" :placeholder="form.kind === 'purchase' ? 'Notes (optional)' : 'Reason (required)'" />
      <button class="btn btn--accent btn--sm" type="submit" :disabled="submitting">Record</button>
    </form>

    <p v-if="loading">Loading…</p>
    <table v-else class="admin-table">
      <thead>
        <tr>
          <th>Name</th><th>Unit</th><th>On hand</th><th>Reserved</th><th>Available</th><th>Reorder point</th><th></th>
        </tr>
      </thead>
      <tbody>
        <template v-for="item in items" :key="item.id">
          <tr :class="{ 'low-stock': item.isLowStock }">
            <td>{{ item.name }}</td>
            <td>{{ item.unit }}</td>
            <td>{{ item.quantityOnHand }}</td>
            <td>{{ item.quantityReserved }}</td>
            <td>{{ item.available }}</td>
            <td>{{ item.reorderPoint }}</td>
            <td><button class="btn btn--outline btn--sm" @click="toggleLedger(item.id)">
              {{ expandedItemId === item.id ? 'Hide ledger' : 'Ledger' }}
            </button></td>
          </tr>
          <tr v-if="expandedItemId === item.id">
            <td colspan="7">
              <p v-if="loadingMovements">Loading ledger…</p>
              <table v-else class="ledger-table">
                <thead>
                  <tr><th>Date</th><th>Type</th><th>Quantity</th><th>By</th><th>Notes</th></tr>
                </thead>
                <tbody>
                  <tr v-for="movement in movements" :key="movement.id">
                    <td>{{ new Date(movement.createdAt).toLocaleString() }}</td>
                    <td>{{ movement.movementType }}</td>
                    <td>{{ movement.quantity }}</td>
                    <td>{{ movement.createdBy }}</td>
                    <td>{{ movement.notes }}</td>
                  </tr>
                  <tr v-if="movements.length === 0"><td colspan="5">No movements yet.</td></tr>
                </tbody>
              </table>
            </td>
          </tr>
        </template>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.admin-stock h1 {
  margin-top: 0;
}

.error {
  color: #b3261e;
}

.movement-form {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  align-items: center;
  margin-bottom: 20px;
}

.movement-form select,
.movement-form input {
  padding: 8px 10px;
  border: 1px solid var(--color-border-idle);
  border-radius: var(--radius-sm);
  font-family: inherit;
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

.low-stock {
  background: oklch(94% 0.06 40);
}

.ledger-table {
  width: 100%;
  border-collapse: collapse;
  margin: 8px 0;
}

.ledger-table th,
.ledger-table td {
  text-align: left;
  padding: 6px 8px;
  font-size: 13px;
  border-bottom: 1px solid var(--color-border-soft);
}
</style>
