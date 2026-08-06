<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue';
import { RouterLink, useRoute } from 'vue-router';
import { useCartStore } from '../stores/cart';
import { getOrder } from '../api/orders';
import type { OrderDto } from '../types/orders';

// Story 10.2 (T10.2.2): the gateway's hosted payment page redirects the browser back here after
// the customer pays or cancels. Paymob's classic integration appends its own transaction query
// params to this URL (merchant_order_id among them) — but this page never trusts those directly;
// it only uses merchant_order_id to know *which* order to ask the API about, then polls the real
// server-confirmed status (the webhook, not this redirect, is what actually confirms payment).
const route = useRoute();
const cart = useCartStore();

const order = ref<OrderDto | null>(null);
const errorMessage = ref('');
const polling = ref(true);

let pollHandle: ReturnType<typeof setTimeout> | null = null;
const MAX_ATTEMPTS = 15;
const POLL_INTERVAL_MS = 2000;

const orderId = (route.query.merchant_order_id ?? route.query.order ?? '') as string;

const isDelivery = computed(() => order.value?.mode === 'Delivery');
const isResolved = computed(() => order.value?.status === 'Confirmed' || order.value?.status === 'Cancelled');
const isConfirmed = computed(() => order.value?.status === 'Confirmed');

async function poll(attemptsLeft: number) {
  if (!orderId) {
    errorMessage.value = "We couldn't tell which order this payment was for. If you were charged, contact us with your payment receipt.";
    polling.value = false;
    return;
  }

  try {
    const result = await getOrder(orderId);
    order.value = result;

    if (result.status === 'Confirmed') {
      cart.clear();
      polling.value = false;
      return;
    }

    if (result.status === 'Cancelled') {
      polling.value = false;
      return;
    }
  } catch (err) {
    errorMessage.value = err instanceof Error ? err.message : 'Could not check your order status.';
  }

  if (attemptsLeft <= 0) {
    polling.value = false;
    return;
  }

  pollHandle = setTimeout(() => poll(attemptsLeft - 1), POLL_INTERVAL_MS);
}

onMounted(() => {
  poll(MAX_ATTEMPTS);
});

onUnmounted(() => {
  if (pollHandle) {
    clearTimeout(pollHandle);
  }
});
</script>

<template>
  <div class="return-page">
    <nav class="return-nav">
      <RouterLink to="/" class="return-nav__brand">CROWD</RouterLink>
    </nav>

    <div class="return-content">
      <template v-if="polling && !isResolved">
        <div class="eyebrow" style="color: var(--color-accent)">Confirming Payment</div>
        <h1 class="return-title">Just a moment…</h1>
        <p class="return-message">We're confirming your payment with the bank — this usually takes a few seconds.</p>
      </template>

      <template v-else-if="isConfirmed && order">
        <div class="eyebrow" style="color: var(--color-accent)">Order Confirmed</div>
        <h1 class="return-title">You are in the crowd.</h1>
        <p class="return-order-number">Order #{{ order.orderNumber }}</p>
        <p class="return-message">
          {{ isDelivery ? 'We are crafting your order now — expect delivery in about 30-40 minutes.' : 'We are crafting your order now — ready for pickup at New Mamsha in about 15-20 minutes.' }}
        </p>
        <RouterLink to="/" class="btn btn--dark">Back to Home</RouterLink>
      </template>

      <template v-else-if="order?.status === 'Cancelled'">
        <div class="eyebrow" style="color: oklch(50% 0.18 30)">Payment Not Completed</div>
        <h1 class="return-title">That didn't go through.</h1>
        <p class="return-message">Your payment wasn't completed, so no order was placed. Your cart is still saved — you can try again.</p>
        <RouterLink to="/checkout" class="btn btn--dark">Back to Checkout</RouterLink>
      </template>

      <template v-else>
        <div class="eyebrow" style="color: oklch(50% 0.18 30)">Still Checking</div>
        <h1 class="return-title">We're not sure yet.</h1>
        <p class="return-message">
          {{ errorMessage || "We couldn't confirm your payment status automatically." }}
          If you were charged, hold onto your payment receipt and contact us.
        </p>
        <RouterLink to="/" class="btn btn--dark">Back to Home</RouterLink>
      </template>
    </div>
  </div>
</template>

<style scoped>
.return-page {
  background: var(--color-bg);
  color: var(--color-ink);
  min-height: 100vh;
}

.return-nav {
  position: sticky;
  top: 0;
  padding: 14px 20px;
  background: color-mix(in oklch, var(--color-bg) 95%, transparent);
  backdrop-filter: blur(8px);
  border-bottom: 1px solid var(--color-border-soft);
}

.return-nav__brand {
  font-family: var(--font-display);
  font-size: 24px;
  color: var(--color-ink);
}

.return-content {
  max-width: 600px;
  margin: 0 auto;
  padding: 96px 32px;
  text-align: center;
}

.return-title {
  font-family: var(--font-display);
  font-size: clamp(32px, 5vw, 48px);
  margin: 0 0 20px;
}

.return-order-number {
  font-size: 17px;
  line-height: 1.6;
  margin: 0 0 8px;
}

.return-message {
  font-size: 16px;
  line-height: 1.6;
  color: var(--color-ink-fainter);
  margin: 0 0 32px;
}
</style>
