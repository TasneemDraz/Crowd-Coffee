<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { RouterLink } from 'vue-router';
import { getDailyDashboard } from '../api/adminDashboard';
import { ApiError } from '../lib/adminApi';
import type { DailyDashboard } from '../types/adminDashboard';

const dashboard = ref<DailyDashboard | null>(null);
const loading = ref(true);
const errorMessage = ref('');

onMounted(async () => {
  try {
    dashboard.value = await getDailyDashboard();
  } catch (err) {
    errorMessage.value = err instanceof ApiError ? err.message : 'Failed to load the dashboard.';
  } finally {
    loading.value = false;
  }
});
</script>

<template>
  <div class="admin-dashboard">
    <h1>Today — {{ dashboard?.date }}</h1>

    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
    <p v-if="loading">Loading…</p>

    <template v-else-if="dashboard">
      <div class="tiles">
        <div class="tile">
          <p class="tile-label">Revenue</p>
          <p class="tile-value">{{ dashboard.revenue.toFixed(2) }} EGP</p>
        </div>
        <div class="tile">
          <p class="tile-label">Orders</p>
          <p class="tile-value">{{ dashboard.orderCount }}</p>
        </div>
        <div class="tile">
          <p class="tile-label">Avg. order value</p>
          <p class="tile-value">{{ dashboard.averageOrderValue.toFixed(2) }} EGP</p>
        </div>
        <div class="tile">
          <p class="tile-label">Delivery / Pickup</p>
          <p class="tile-value">{{ dashboard.deliveryCount }} / {{ dashboard.pickupCount }}</p>
        </div>
        <RouterLink to="/admin/stock" class="tile tile--link" :class="{ 'tile--alert': dashboard.lowStockCount > 0 }">
          <p class="tile-label">Low stock</p>
          <p class="tile-value">{{ dashboard.lowStockCount }}</p>
        </RouterLink>
        <RouterLink to="/admin/reconciliation" class="tile tile--link">
          <p class="tile-label">Reconciliation</p>
          <p class="tile-value">View →</p>
        </RouterLink>
      </div>

      <h2>Top sellers today</h2>
      <ol v-if="dashboard.topSellingItems.length" class="top-sellers">
        <li v-for="item in dashboard.topSellingItems" :key="item.productName">
          {{ item.productName }} — {{ item.quantitySold }} sold
        </li>
      </ol>
      <p v-else>No sales yet today.</p>
    </template>
  </div>
</template>

<style scoped>
.admin-dashboard h1 {
  margin-top: 0;
}

.error {
  color: #b3261e;
}

.tiles {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
  gap: 12px;
  margin-bottom: 32px;
}

.tile {
  background: var(--color-white);
  border: 1px solid var(--color-border-soft);
  border-radius: var(--radius-md);
  padding: 16px;
}

.tile-label {
  margin: 0 0 4px;
  font-size: 13px;
  color: var(--color-ink-faint);
}

.tile-value {
  margin: 0;
  font-size: 24px;
  font-weight: 700;
}

.tile--link {
  display: block;
  color: inherit;
}

.tile--link:hover {
  border-color: var(--color-accent);
}

.tile--alert .tile-value {
  color: #b3261e;
}

.top-sellers {
  padding-left: 20px;
}
</style>
