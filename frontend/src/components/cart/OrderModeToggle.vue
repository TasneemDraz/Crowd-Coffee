<script setup lang="ts">
import type { OrderMode } from '../../stores/cart';

defineProps<{ mode: OrderMode }>();
const emit = defineEmits<{ select: [mode: OrderMode] }>();
</script>

<template>
  <div class="order-mode">
    <h3>How would you like it?</h3>
    <div class="order-mode__buttons">
      <button
        type="button"
        class="order-mode__btn"
        :class="{ 'order-mode__btn--active': mode === 'delivery' }"
        @click="emit('select', 'delivery')"
      >
        Delivery
      </button>
      <button
        type="button"
        class="order-mode__btn"
        :class="{ 'order-mode__btn--active': mode === 'pickup' }"
        @click="emit('select', 'pickup')"
      >
        Pickup
      </button>
    </div>
    <p v-if="mode === 'pickup'" class="order-mode__note">
      Pickup at CROWD — New Mamsha Corniche, Port Said.
    </p>
  </div>
</template>

<style scoped>
.order-mode {
  background: white;
  border-radius: var(--radius-sm);
  padding: 24px;
  margin-bottom: 20px;
}

.order-mode h3 {
  margin: 0 0 16px;
  font-size: 15px;
  font-weight: 700;
}

.order-mode__buttons {
  display: flex;
  gap: 12px;
}

.order-mode__btn {
  flex: 1;
  padding: 14px;
  border-radius: 12px;
  border: 1.5px solid var(--color-border-idle);
  background: white;
  color: var(--color-ink);
  font-weight: 600;
  cursor: pointer;
}

.order-mode__btn--active {
  border-color: var(--color-accent);
  background: var(--color-accent-surface);
  /* Story 16.3 (T16.3.3): accent-hover, not accent — see KitchenBoardView's identical fix. */
  color: var(--color-accent-hover);
}

.order-mode__note {
  margin: 16px 0 0;
  font-size: 14px;
  color: var(--color-ink-fainter);
}
</style>
