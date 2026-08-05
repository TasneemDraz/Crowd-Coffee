<script setup lang="ts">
import { RouterLink } from 'vue-router';

defineProps<{ count: number; subtotal: number }>();
</script>

<template>
  <RouterLink v-if="count > 0" to="/cart" class="cart-bar">
    <span>View Cart · {{ count }} item(s)</span>
    <span>{{ subtotal }} EGP →</span>
  </RouterLink>
</template>

<style scoped>
.cart-bar {
  position: fixed;
  left: 24px;
  right: 24px;
  bottom: 24px;
  max-width: 850px;
  margin: 0 auto;
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: var(--color-ink);
  color: white;
  padding: 18px 26px;
  border-radius: var(--radius-pill);
  font-weight: 600;
  box-shadow: 0 12px 32px rgba(0, 0, 0, 0.25);
  z-index: 60;
}
</style>
