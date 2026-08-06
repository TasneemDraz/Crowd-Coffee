<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue';
import { RouterLink } from 'vue-router';
import { useCartStore } from '../stores/cart';
import { useCatalog } from '../composables/useCatalog';
import { useModifiers } from '../composables/useModifiers';
import { decomposeDrinkLineId } from '../lib/cartLineId';
import { createOrder, ApiError } from '../api/orders';
import { initiatePayment } from '../api/payments';
import type { CreateOrderLineRequest, OrderDto, PaymentMethod } from '../types/orders';

const cart = useCartStore();
const { products, ensureLoaded } = useCatalog();
const { getForCategory } = useModifiers();

const name = ref('');
const phone = ref('');
const address = ref('');
const payment = ref<'cash' | 'card' | 'wallet'>('cash');
const lat = ref<number | null>(null);
const lng = ref<number | null>(null);

const submitting = ref(false);
const errorMessage = ref('');
const confirmedOrder = ref<OrderDto | null>(null);

// Stable for the lifetime of this checkout attempt (Story 9.3) — a retried submission after a
// network failure reuses the same key rather than risking a duplicate order.
const idempotencyKey = crypto.randomUUID();

const isDelivery = computed(() => cart.orderMode === 'delivery');
const hasItems = computed(() => cart.lines.length > 0);
const paymentMethodApi = computed<PaymentMethod>(() => {
  if (payment.value === 'card') return 'Card';
  if (payment.value === 'wallet') return 'Wallet';
  return 'Cash';
});

function onLocationMessage(event: MessageEvent) {
  if (event.data && event.data.type === 'crowd-location') {
    lat.value = event.data.lat;
    lng.value = event.data.lng;
  }
}

onMounted(() => {
  window.addEventListener('message', onLocationMessage);
  ensureLoaded().catch(() => {
    // Menu fetch failure surfaces when the customer tries to submit (buildOrderLines throws);
    // nothing actionable to show before that.
  });
});

onUnmounted(() => {
  window.removeEventListener('message', onLocationMessage);
});

async function buildOrderLines(): Promise<CreateOrderLineRequest[]> {
  await ensureLoaded();

  const lines: CreateOrderLineRequest[] = [];
  for (const line of cart.lines) {
    const decomposed = decomposeDrinkLineId(line.id);

    if (decomposed.milkOptionId === null) {
      // Non-drink line: the id is just the bare product id (Epic 6/7's plain cart.add()).
      lines.push({ productId: decomposed.productId, milkOptionId: null, shotOptionId: null, syrupOptionIds: [], quantity: line.qty });
      continue;
    }

    const product = products.value.find((p) => p.id === decomposed.productId);
    if (!product) {
      throw new Error(`"${line.name}" is no longer on the menu — please remove it from your cart and try again.`);
    }

    let shotOptionId: string | null = null;
    if (decomposed.hasShot) {
      const groups = await getForCategory(product.categorySlug, product.slug);
      shotOptionId = groups.find((g) => g.slug === 'shot')?.options[0]?.id ?? null;
    }

    lines.push({
      productId: decomposed.productId,
      milkOptionId: decomposed.milkOptionId,
      shotOptionId,
      syrupOptionIds: decomposed.syrupOptionIds,
      quantity: line.qty,
    });
  }

  return lines;
}

async function placeOrder() {
  errorMessage.value = '';

  if (!name.value.trim() || !phone.value.trim() || (isDelivery.value && !address.value.trim())) {
    errorMessage.value = `Please fill in your name, phone${isDelivery.value ? ' and delivery address.' : '.'}`;
    return;
  }

  submitting.value = true;
  try {
    const lines = await buildOrderLines();
    const order = await createOrder({
      customerName: name.value.trim(),
      phone: phone.value.trim(),
      mode: isDelivery.value ? 'Delivery' : 'Pickup',
      address: isDelivery.value ? address.value.trim() : null,
      latitude: lat.value,
      longitude: lng.value,
      paymentMethod: paymentMethodApi.value,
      idempotencyKey,
      lines,
      notes: null,
    });

    if (order.paymentMethod === 'Cash') {
      cart.clear();
      confirmedOrder.value = order;
      return;
    }

    // Card/Wallet: order was created at PendingPayment (§7) — hand off to the gateway's hosted
    // page (§9 deviation from the mock's inline card fields, Epic 10.2). The cart clears only
    // once payment is actually confirmed (the return page / webhook), not here.
    const { redirectUrl } = await initiatePayment(order.id);
    window.location.href = redirectUrl;
  } catch (err) {
    errorMessage.value = err instanceof ApiError ? err.message : err instanceof Error ? err.message : 'Something went wrong placing your order.';
  } finally {
    submitting.value = false;
  }
}

const confirmationMessage = computed(() => {
  if (!confirmedOrder.value) return '';
  return isDelivery.value
    ? 'We are crafting your order now — expect delivery in about 30-40 minutes.'
    : 'We are crafting your order now — ready for pickup at New Mamsha in about 15-20 minutes.';
});

const deliverySummary = computed(() => {
  if (!confirmedOrder.value) return '';
  return isDelivery.value
    ? `Delivering to ${confirmedOrder.value.address ?? ''}${confirmedOrder.value.latitude != null ? ' — pinned on the map.' : '.'}`
    : 'Ready for pickup at New Mamsha Corniche.';
});
</script>

<template>
  <div class="checkout-page">
    <nav class="checkout-nav">
      <RouterLink to="/" class="checkout-nav__brand">CROWD</RouterLink>
      <RouterLink to="/cart" class="checkout-nav__cart-link">&larr; Cart</RouterLink>
    </nav>

    <template v-if="confirmedOrder">
      <div class="checkout-confirmation">
        <div class="eyebrow" style="color: var(--color-accent)">Order Confirmed</div>
        <h1 class="checkout-confirmation__title">You are in the crowd.</h1>
        <p class="checkout-confirmation__number">Order #{{ confirmedOrder.orderNumber }}</p>
        <p class="checkout-confirmation__summary">{{ deliverySummary }}</p>
        <p class="checkout-confirmation__message">{{ confirmationMessage }}</p>
        <RouterLink to="/" class="btn btn--dark">Back to Home</RouterLink>
      </div>
    </template>

    <template v-else-if="!hasItems">
      <div class="checkout-empty">
        <p>Your cart is empty.</p>
        <RouterLink to="/menu" class="btn btn--dark">Browse the Menu</RouterLink>
      </div>
    </template>

    <template v-else>
      <div class="checkout-content">
        <div class="checkout-main">
          <div class="eyebrow" style="color: var(--color-accent)">Checkout</div>
          <h1 class="checkout-title">Almost there</h1>

          <div class="checkout-card">
            <h3>Your details</h3>
            <input v-model="name" placeholder="Full name" class="checkout-input" />
            <input v-model="phone" placeholder="Phone number" class="checkout-input" />

            <template v-if="isDelivery">
              <input v-model="address" placeholder="Delivery address" class="checkout-input checkout-input--spaced" />
              <p class="checkout-hint">Pin your exact location so the rider finds you fast:</p>
              <iframe src="/location-picker.html" title="Pin your delivery location" class="checkout-location-frame"></iframe>
              <p v-if="lat !== null" class="checkout-pin-label">Pinned: {{ lat!.toFixed(5) }}, {{ lng!.toFixed(5) }}</p>
            </template>
          </div>

          <div class="checkout-card">
            <h3>Payment method</h3>
            <div class="checkout-payment-options">
              <button
                type="button"
                class="checkout-payment-btn"
                :class="{ 'checkout-payment-btn--active': payment === 'cash' }"
                @click="payment = 'cash'"
              >
                Cash on delivery / pickup
              </button>
              <button
                type="button"
                class="checkout-payment-btn"
                :class="{ 'checkout-payment-btn--active': payment === 'card' }"
                @click="payment = 'card'"
              >
                Card payment
              </button>
              <button
                type="button"
                class="checkout-payment-btn"
                :class="{ 'checkout-payment-btn--active': payment === 'wallet' }"
                @click="payment = 'wallet'"
              >
                Mobile wallet (Vodafone Cash / InstaPay)
              </button>
            </div>
            <p v-if="payment !== 'cash'" class="checkout-hint checkout-hint--spaced">
              You'll be redirected to our payment provider to complete payment securely — we never see or store your card details.
            </p>
          </div>

          <p v-if="errorMessage" class="checkout-error">{{ errorMessage }}</p>

          <button type="button" class="btn btn--accent checkout-submit" :disabled="submitting" @click="placeOrder">
            {{ submitting ? 'Placing order…' : `Place Order · ${cart.total} EGP` }}
          </button>
        </div>

        <div class="checkout-summary">
          <div class="checkout-summary__card">
            <h3>Order summary</h3>
            <div v-for="line in cart.lines" :key="line.id" class="checkout-summary__row">
              <span>{{ line.qty }}&times; {{ line.name }}</span>
              <span class="checkout-summary__amount">{{ line.price * line.qty }} EGP</span>
            </div>
            <div class="checkout-summary__row checkout-summary__row--muted checkout-summary__row--bordered">
              <span>Subtotal</span><span>{{ cart.subtotal }} EGP</span>
            </div>
            <div class="checkout-summary__row checkout-summary__row--muted">
              <span>{{ isDelivery ? 'Delivery fee' : 'Pickup' }}</span>
              <span>{{ cart.deliveryFee === 0 ? 'Free' : `${cart.deliveryFee} EGP` }}</span>
            </div>
            <div class="checkout-summary__row checkout-summary__row--total">
              <span>Total</span><span>{{ cart.total }} EGP</span>
            </div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
.checkout-page {
  background: var(--color-bg);
  color: var(--color-ink);
  min-height: 100vh;
}

.checkout-nav {
  position: sticky;
  top: 0;
  z-index: 50;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 14px 20px;
  background: color-mix(in oklch, var(--color-bg) 95%, transparent);
  backdrop-filter: blur(8px);
  border-bottom: 1px solid var(--color-border-soft);
  flex-wrap: wrap;
}

.checkout-nav__brand {
  font-family: var(--font-display);
  font-size: 24px;
  color: var(--color-ink);
}

.checkout-nav__cart-link {
  font-weight: 600;
  font-size: 13px;
  color: var(--color-ink);
}

.checkout-content {
  max-width: 1000px;
  margin: 0 auto;
  padding: 24px 20px 96px;
  display: flex;
  gap: 32px;
  flex-wrap: wrap;
}

.checkout-main {
  flex: 2;
  min-width: 320px;
}

.checkout-title {
  font-family: var(--font-display);
  font-size: clamp(28px, 4vw, 40px);
  margin: 0 0 32px;
}

.checkout-card {
  background: white;
  border-radius: var(--radius-sm);
  padding: 24px;
  margin-bottom: 20px;
}

.checkout-card h3 {
  margin: 0 0 16px;
  font-size: 15px;
  font-weight: 700;
}

.checkout-input {
  width: 100%;
  padding: 14px;
  border-radius: 10px;
  border: 1px solid var(--color-border-idle);
  font-size: 15px;
  font-family: inherit;
  margin-bottom: 12px;
  box-sizing: border-box;
}

.checkout-input--spaced {
  margin-top: 0;
}

.checkout-hint {
  font-size: 13px;
  color: var(--color-ink-fainter);
  margin: 14px 0 8px;
}

.checkout-hint--spaced {
  margin-top: 16px;
}

.checkout-location-frame {
  width: 100%;
  height: 280px;
  border: none;
  border-radius: 12px;
  display: block;
}

.checkout-pin-label {
  font-size: 13px;
  color: var(--color-accent);
  font-weight: 600;
  margin: 10px 0 0;
}

.checkout-payment-options {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.checkout-payment-btn {
  text-align: left;
  padding: 14px 16px;
  border-radius: 12px;
  border: 1.5px solid var(--color-border-idle);
  background: white;
  color: var(--color-ink);
  font-weight: 600;
  cursor: pointer;
}

.checkout-payment-btn--active {
  border-color: var(--color-accent);
  background: var(--color-accent-surface);
  color: var(--color-accent);
}

.checkout-error {
  color: oklch(50% 0.18 30);
  font-size: 14px;
  margin: 0 0 16px;
}

.checkout-submit {
  width: 100%;
}

.checkout-submit:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.checkout-summary {
  flex: 1;
  min-width: 260px;
}

.checkout-summary__card {
  background: white;
  border-radius: var(--radius-sm);
  padding: 24px;
  position: sticky;
  top: 96px;
}

.checkout-summary__card h3 {
  margin: 0 0 16px;
  font-size: 15px;
  font-weight: 700;
}

.checkout-summary__row {
  display: flex;
  justify-content: space-between;
  font-size: 14px;
  margin-bottom: 10px;
  gap: 8px;
}

.checkout-summary__amount {
  white-space: nowrap;
}

.checkout-summary__row--muted {
  color: var(--color-ink-fainter);
}

.checkout-summary__row--bordered {
  margin-top: 16px;
  border-top: 1px solid var(--color-border-soft);
  padding-top: 16px;
}

.checkout-summary__row--total {
  font-size: 17px;
  font-weight: 700;
  margin-top: 12px;
}

.checkout-empty,
.checkout-confirmation {
  max-width: 500px;
  margin: 0 auto;
  padding: 96px 32px;
  text-align: center;
}

.checkout-confirmation {
  max-width: 600px;
}

.checkout-confirmation__title {
  font-family: var(--font-display);
  font-size: clamp(32px, 5vw, 48px);
  margin: 0 0 20px;
}

.checkout-confirmation__number {
  font-size: 17px;
  line-height: 1.6;
  margin: 0 0 8px;
}

.checkout-confirmation__summary {
  font-size: 15px;
  line-height: 1.6;
  color: var(--color-ink-fainter);
  margin: 0 0 8px;
}

.checkout-confirmation__message {
  font-size: 16px;
  line-height: 1.6;
  color: var(--color-ink-fainter);
  margin: 0 0 32px;
}

.checkout-empty p {
  font-size: 18px;
  margin: 0 0 20px;
  color: var(--color-ink-fainter);
}
</style>
