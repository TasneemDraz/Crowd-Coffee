<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue';
import type { HubConnection } from '@microsoft/signalr';
import { HubConnectionState } from '@microsoft/signalr';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/auth';
import { advanceOrder, connectOrderHub, getKitchenBoard, ApiError } from '../api/kitchen';
import { playNewOrderAlert, playOrderCancelledAlert } from '../lib/alertSound';
import type { KitchenOrder } from '../types/kitchen';

const BOARD_STATUSES = ['Confirmed', 'Preparing', 'Ready'] as const;

const auth = useAuthStore();
const router = useRouter();

const orders = ref<KitchenOrder[]>([]);
const loadError = ref('');
const advancingId = ref<string | null>(null);
let hubConnection: HubConnection | null = null;

const columns = computed(() => ({
  Confirmed: orders.value.filter((o) => o.status === 'Confirmed').sort(byCreatedAt),
  Preparing: orders.value.filter((o) => o.status === 'Preparing').sort(byCreatedAt),
  Ready: orders.value.filter((o) => o.status === 'Ready').sort(byCreatedAt),
}));

function byCreatedAt(a: KitchenOrder, b: KitchenOrder) {
  return new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
}

function nextActionLabel(order: KitchenOrder): string {
  if (order.status === 'Confirmed') return 'Start preparing';
  if (order.status === 'Preparing') return 'Mark ready';
  if (order.status === 'Ready') return order.mode === 'Delivery' ? 'Send out for delivery' : 'Mark picked up';
  return 'Advance';
}

function mapsUrl(order: KitchenOrder): string | null {
  if (order.mode !== 'Delivery' || order.latitude === null || order.longitude === null) {
    return null;
  }
  return `https://www.google.com/maps/dir/?api=1&destination=${order.latitude},${order.longitude}`;
}

function applyIncomingOrder(order: KitchenOrder) {
  const existingIndex = orders.value.findIndex((o) => o.id === order.id);
  const isNewArrival = existingIndex === -1 && order.status === 'Confirmed';

  if (!(BOARD_STATUSES as readonly string[]).includes(order.status)) {
    // Left the board (picked up, out for delivery, delivered, cancelled, ...).
    if (existingIndex !== -1) {
      // Story 15.1: only Cancelled means "a barista may already be mid-prep on this — stop."
      // PickedUp/OutForDelivery/Delivered are normal, expected departures from the board and
      // don't warrant an alert (advancing an order yourself already gives instant visual
      // feedback via applyIncomingOrder's own optimistic call in advance()).
      if (order.status === 'Cancelled') {
        playOrderCancelledAlert();
      }
      orders.value.splice(existingIndex, 1);
    }
    return;
  }

  if (existingIndex !== -1) {
    orders.value.splice(existingIndex, 1, order);
  } else {
    orders.value.push(order);
  }

  if (isNewArrival) {
    playNewOrderAlert();
  }
}

async function advance(order: KitchenOrder) {
  advancingId.value = order.id;
  try {
    const updated = await advanceOrder(order.id);
    applyIncomingOrder(updated); // the hub push also arrives, but this makes the tap feel instant
  } catch (err) {
    loadError.value = err instanceof ApiError ? err.message : 'Failed to advance the order.';
  } finally {
    advancingId.value = null;
  }
}

async function logout() {
  hubConnection?.stop();
  await auth.logout();
  await router.replace('/staff/login');
}

onMounted(async () => {
  try {
    orders.value = await getKitchenBoard();
  } catch (err) {
    loadError.value = err instanceof ApiError ? err.message : 'Failed to load the kitchen board.';
  }

  hubConnection = connectOrderHub(applyIncomingOrder);
  hubConnection.start().catch(() => {
    loadError.value = 'Live updates are unavailable — reload to refresh manually.';
  });
});

onUnmounted(() => {
  if (hubConnection && hubConnection.state !== HubConnectionState.Disconnected) {
    hubConnection.stop();
  }
});
</script>

<template>
  <div class="board-page">
    <header class="board-header">
      <div>
        <p class="eyebrow">Kitchen board</p>
        <h1>{{ auth.username }} · {{ auth.role }}</h1>
      </div>
      <button class="btn btn--outline btn--sm" @click="logout">Sign out</button>
    </header>

    <p v-if="loadError" class="error">{{ loadError }}</p>

    <div class="columns">
      <section v-for="status in BOARD_STATUSES" :key="status" class="column">
        <h2>{{ status === 'Confirmed' ? 'New' : status }} ({{ columns[status].length }})</h2>

        <article v-for="order in columns[status]" :key="order.id" class="card">
          <div class="card-head">
            <strong>{{ order.orderNumber }}</strong>
            <span class="tag-pill">{{ order.mode }}</span>
          </div>

          <p class="customer">{{ order.customerName }} · {{ order.phone }}</p>
          <a v-if="mapsUrl(order)" :href="mapsUrl(order)!" target="_blank" rel="noopener" class="maps-link">
            📍 {{ order.address }}
          </a>

          <ul class="lines">
            <li v-for="(line, index) in order.lines" :key="index">
              {{ line.quantity }}× {{ line.productName }}
              <span v-if="line.modifiers.length" class="modifiers">({{ line.modifiers.join(', ') }})</span>
            </li>
          </ul>

          <p v-if="order.notes" class="notes">Note: {{ order.notes }}</p>

          <button
            class="btn btn--accent btn--sm"
            :disabled="advancingId === order.id"
            @click="advance(order)"
          >
            {{ advancingId === order.id ? 'Updating…' : nextActionLabel(order) }}
          </button>
        </article>

        <p v-if="columns[status].length === 0" class="empty">No orders.</p>
      </section>
    </div>
  </div>
</template>

<style scoped>
.board-page {
  min-height: 100vh;
  background: var(--color-bg);
  color: var(--color-ink);
  padding: 24px;
}

.board-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
}

.board-header h1 {
  margin: 4px 0 0;
  font-size: 22px;
}

.eyebrow {
  color: var(--color-accent);
  margin: 0;
}

.error {
  color: #b3261e;
  margin-bottom: 16px;
}

.columns {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: 16px;
  align-items: start;
}

.column h2 {
  font-size: 16px;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  margin: 0 0 12px;
}

.card {
  background: var(--color-white);
  border: 1px solid var(--color-border-soft);
  border-radius: var(--radius-md);
  padding: 16px;
  margin-bottom: 12px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.card-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.card-head .tag-pill {
  background: var(--color-accent-surface);
  /* Story 16.3 (T16.3.3): --color-accent text on --color-accent-surface measures ~4.1:1,
     just under AA's 4.5:1 for this pill's small text — accent-hover clears it (~6.3:1). */
  color: var(--color-accent-hover);
}

.customer {
  margin: 0;
  font-size: 14px;
  color: var(--color-ink-soft);
}

.maps-link {
  font-size: 14px;
}

.lines {
  list-style: none;
  padding: 0;
  margin: 0;
  font-size: 14px;
}

.modifiers {
  color: var(--color-ink-faint);
}

.notes {
  font-size: 13px;
  font-style: italic;
  margin: 0;
}

.empty {
  color: var(--color-ink-fainter);
  font-size: 14px;
}

.btn--sm {
  align-self: flex-start;
}
</style>
