<script setup lang="ts">
import type { CartLine } from '../../stores/cart';
import CartQtyStepper from './CartQtyStepper.vue';

defineProps<{ line: CartLine }>();
const emit = defineEmits<{ increase: []; decrease: []; remove: [] }>();
</script>

<template>
  <div class="cart-line">
    <div class="cart-line__info">
      <h3>{{ line.name }}</h3>
      <span class="cart-line__unit-price">{{ line.price }} EGP each</span>
    </div>
    <div class="cart-line__controls">
      <CartQtyStepper :qty="line.qty" @increase="emit('increase')" @decrease="emit('decrease')" />
      <span class="cart-line__total">{{ line.price * line.qty }} EGP</span>
      <button type="button" class="cart-line__remove" @click="emit('remove')">Remove</button>
    </div>
  </div>
</template>

<style scoped>
.cart-line {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  padding: 16px 18px;
  background: white;
  border-radius: var(--radius-sm);
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.05);
  flex-wrap: wrap;
}

.cart-line__info {
  min-width: 0;
}

.cart-line__info h3 {
  margin: 0 0 4px;
  font-size: 15px;
}

.cart-line__unit-price {
  color: var(--color-ink-fainter);
  font-size: 13px;
}

.cart-line__controls {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.cart-line__total {
  min-width: 64px;
  text-align: right;
  font-weight: 700;
  font-size: 14px;
}

.cart-line__remove {
  background: none;
  border: none;
  color: oklch(55% 0.01 50);
  font-size: 12px;
  cursor: pointer;
  text-decoration: underline;
  margin-left: 2px;
  flex-shrink: 0;
}
</style>
