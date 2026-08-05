<script setup lang="ts">
import { RouterLink } from 'vue-router';
import { useCartStore } from '../stores/cart';
import type { OrderMode } from '../stores/cart';
import CartLineItem from '../components/cart/CartLineItem.vue';
import OrderModeToggle from '../components/cart/OrderModeToggle.vue';

const cart = useCartStore();

function handleModeSelect(mode: OrderMode) {
  cart.setOrderMode(mode);
}
</script>

<template>
  <div class="cart-page">
    <nav class="cart-nav">
      <RouterLink to="/" class="cart-nav__brand">CROWD</RouterLink>
      <RouterLink to="/menu" class="cart-nav__menu-link">Menu →</RouterLink>
    </nav>

    <div class="cart-content">
      <div class="eyebrow" style="color: var(--color-accent)">Your Order</div>
      <h1 class="cart-title">Cart</h1>

      <template v-if="cart.lines.length > 0">
        <div class="cart-lines">
          <CartLineItem
            v-for="line in cart.lines"
            :key="line.id"
            :line="line"
            @increase="cart.changeQty(line.id, 1)"
            @decrease="cart.changeQty(line.id, -1)"
            @remove="cart.removeLine(line.id)"
          />
        </div>

        <OrderModeToggle :mode="cart.orderMode" @select="handleModeSelect" />

        <div class="cart-totals">
          <div class="cart-totals__row">
            <span>Subtotal</span>
            <span>{{ cart.subtotal }} EGP</span>
          </div>
          <div class="cart-totals__row cart-totals__row--muted">
            <span>{{ cart.orderMode === 'pickup' ? 'Pickup' : 'Delivery fee' }}</span>
            <span>{{ cart.deliveryFee === 0 ? 'Free' : `${cart.deliveryFee} EGP` }}</span>
          </div>
          <div class="cart-totals__row cart-totals__row--total">
            <span>Total</span>
            <span>{{ cart.total }} EGP</span>
          </div>
          <RouterLink to="/checkout" class="btn btn--accent cart-checkout-btn">Proceed to Checkout</RouterLink>
        </div>
      </template>

      <div v-else class="cart-empty">
        <p>Your cart is empty.</p>
        <RouterLink to="/menu" class="btn btn--dark">Browse the Menu</RouterLink>
      </div>
    </div>
  </div>
</template>

<style scoped>
.cart-page {
  background: var(--color-bg);
  color: var(--color-ink);
  min-height: 100vh;
}

.cart-nav {
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

.cart-nav__brand {
  font-family: var(--font-display);
  font-size: 24px;
  color: var(--color-ink);
}

.cart-nav__menu-link {
  font-weight: 600;
  font-size: 13px;
  color: var(--color-ink);
}

.cart-content {
  padding: 28px 20px 96px;
  max-width: 800px;
  margin: 0 auto;
}

.cart-title {
  font-family: var(--font-display);
  font-size: clamp(32px, 5vw, 44px);
  margin: 0 0 32px;
}

.cart-lines {
  display: flex;
  flex-direction: column;
  gap: 14px;
  margin-bottom: 24px;
}

.cart-totals {
  background: white;
  border-radius: var(--radius-sm);
  padding: 24px;
}

.cart-totals__row {
  display: flex;
  justify-content: space-between;
  margin-bottom: 10px;
  font-size: 15px;
}

.cart-totals__row--muted {
  margin-bottom: 16px;
  color: var(--color-ink-fainter);
}

.cart-totals__row--total {
  margin-bottom: 24px;
  font-size: 18px;
  font-weight: 700;
  border-top: 1px solid var(--color-border-soft);
  padding-top: 16px;
}

.cart-checkout-btn {
  display: block;
  width: 100%;
}

.cart-empty {
  text-align: center;
  padding: 80px 20px;
  background: white;
  border-radius: var(--radius-sm);
}

.cart-empty p {
  font-size: 18px;
  margin: 0 0 20px;
  color: var(--color-ink-fainter);
}
</style>
