<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { getOrderDetail, searchOrders } from '../api/adminOrders';
import { ApiError } from '../lib/adminApi';
import type { AdminOrderDetail, AdminOrderSummary } from '../types/adminOrders';

const filters = ref({ dateFrom: '', dateTo: '', status: '', mode: '', paymentMethod: '' });
const orders = ref<AdminOrderSummary[]>([]);
const loading = ref(true);
const errorMessage = ref('');

const selectedOrder = ref<AdminOrderDetail | null>(null);
const loadingDetail = ref(false);

async function runSearch() {
  loading.value = true;
  errorMessage.value = '';
  try {
    orders.value = await searchOrders({
      dateFrom: filters.value.dateFrom || null,
      dateTo: filters.value.dateTo || null,
      status: filters.value.status || null,
      mode: filters.value.mode || null,
      paymentMethod: filters.value.paymentMethod || null,
    });
  } catch (err) {
    errorMessage.value = err instanceof ApiError ? err.message : 'Failed to load orders.';
  } finally {
    loading.value = false;
  }
}

onMounted(runSearch);

async function openDetail(orderId: string) {
  loadingDetail.value = true;
  errorMessage.value = '';
  try {
    selectedOrder.value = await getOrderDetail(orderId);
  } catch (err) {
    errorMessage.value = err instanceof ApiError ? err.message : 'Failed to load order detail.';
  } finally {
    loadingDetail.value = false;
  }
}
</script>

<template>
  <div class="admin-orders">
    <h1>Order history</h1>

    <form class="filters" @submit.prevent="runSearch">
      <label>From <input v-model="filters.dateFrom" type="date" /></label>
      <label>To <input v-model="filters.dateTo" type="date" /></label>
      <select v-model="filters.status">
        <option value="">Any status</option>
        <option v-for="s in ['Pending','PendingPayment','Confirmed','Preparing','Ready','OutForDelivery','Delivered','PickedUp','Cancelled']" :key="s" :value="s">{{ s }}</option>
      </select>
      <select v-model="filters.mode">
        <option value="">Any mode</option>
        <option value="Delivery">Delivery</option>
        <option value="Pickup">Pickup</option>
      </select>
      <select v-model="filters.paymentMethod">
        <option value="">Any payment</option>
        <option value="Cash">Cash</option>
        <option value="Card">Card</option>
        <option value="Wallet">Wallet</option>
      </select>
      <button class="btn btn--accent btn--sm" type="submit">Search</button>
    </form>

    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>

    <div class="layout">
      <p v-if="loading">Loading…</p>
      <table v-else class="admin-table">
        <thead>
          <tr><th>Order #</th><th>Customer</th><th>Mode</th><th>Payment</th><th>Status</th><th>Total</th><th>Placed</th></tr>
        </thead>
        <tbody>
          <tr v-for="order in orders" :key="order.id" class="clickable" @click="openDetail(order.id)">
            <td>{{ order.orderNumber }}</td>
            <td>{{ order.customerName }}</td>
            <td>{{ order.mode }}</td>
            <td>{{ order.paymentMethod }}</td>
            <td>{{ order.status }}</td>
            <td>{{ order.total }} EGP</td>
            <td>{{ new Date(order.createdAt).toLocaleString() }}</td>
          </tr>
          <tr v-if="orders.length === 0"><td colspan="7">No orders match these filters.</td></tr>
        </tbody>
      </table>

      <aside v-if="selectedOrder || loadingDetail" class="detail-panel">
        <button class="btn btn--outline btn--sm" @click="selectedOrder = null">Close</button>
        <p v-if="loadingDetail">Loading…</p>
        <template v-else-if="selectedOrder">
          <h2>{{ selectedOrder.orderNumber }}</h2>
          <p>{{ selectedOrder.customerName }} · {{ selectedOrder.phone }}</p>
          <p v-if="selectedOrder.address">{{ selectedOrder.address }}</p>
          <p>{{ selectedOrder.mode }} · {{ selectedOrder.paymentMethod }} ({{ selectedOrder.paymentStatus }})</p>

          <ul class="lines">
            <li v-for="(line, i) in selectedOrder.lines" :key="i">
              {{ line.quantity }}× {{ line.productName }} — {{ line.lineTotal }} EGP
              <span v-if="line.modifiers.length" class="modifiers">({{ line.modifiers.join(', ') }})</span>
            </li>
          </ul>
          <p class="total">Subtotal {{ selectedOrder.subtotal }} + Delivery {{ selectedOrder.deliveryFee }} = <strong>{{ selectedOrder.total }} EGP</strong></p>
          <p v-if="selectedOrder.notes">Note: {{ selectedOrder.notes }}</p>

          <h3>Status history</h3>
          <ol class="history">
            <li v-for="(entry, i) in selectedOrder.statusHistory" :key="i">
              <strong>{{ entry.fromStatus }} → {{ entry.toStatus }}</strong>
              <span class="meta">{{ new Date(entry.changedAt).toLocaleString() }} · {{ entry.changedBy }}</span>
              <span v-if="entry.reason" class="reason">{{ entry.reason }}</span>
            </li>
          </ol>
        </template>
      </aside>
    </div>
  </div>
</template>

<style scoped>
.admin-orders h1 {
  margin-top: 0;
}

.error {
  color: #b3261e;
}

.filters {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  align-items: center;
  margin-bottom: 20px;
}

.filters label {
  display: flex;
  flex-direction: column;
  font-size: 12px;
  gap: 2px;
}

.filters input,
.filters select {
  padding: 8px 10px;
  border: 1px solid var(--color-border-idle);
  border-radius: var(--radius-sm);
  font-family: inherit;
}

.layout {
  display: flex;
  gap: 20px;
  align-items: flex-start;
}

.admin-table {
  flex: 1;
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

.clickable {
  cursor: pointer;
}

.clickable:hover {
  background: var(--color-bg-tint);
}

.detail-panel {
  width: 340px;
  flex-shrink: 0;
  background: var(--color-white);
  border: 1px solid var(--color-border-soft);
  border-radius: var(--radius-md);
  padding: 16px;
}

.detail-panel h2 {
  margin-top: 0;
}

.lines {
  list-style: none;
  padding: 0;
  font-size: 14px;
}

.modifiers {
  color: var(--color-ink-faint);
}

.total {
  font-size: 14px;
}

.history {
  padding-left: 18px;
  font-size: 13px;
}

.history li {
  margin-bottom: 8px;
}

.history .meta {
  display: block;
  color: var(--color-ink-faint);
}

.history .reason {
  display: block;
  font-style: italic;
}
</style>
