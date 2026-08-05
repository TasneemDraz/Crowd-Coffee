<script setup lang="ts">
import type { ProductDto } from '../../types/catalog';

defineProps<{ product: ProductDto }>();
const emit = defineEmits<{ add: [product: ProductDto] }>();
</script>

<template>
  <div class="product-card">
    <div class="product-card__body">
      <h3>{{ product.name }}</h3>
      <p v-if="product.description">{{ product.description }}</p>
      <span class="product-card__price">{{ product.price }} EGP</span>
    </div>
    <button type="button" class="btn btn--dark btn--sm" @click="emit('add', product)">Add</button>
  </div>
</template>

<style scoped>
.product-card {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 14px;
  padding: 18px;
  background: white;
  border-radius: var(--radius-sm);
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.05);
  flex-wrap: wrap;
}

.product-card__body {
  flex: 1;
  min-width: 0;
}

.product-card__body h3 {
  margin: 0 0 4px;
  font-size: 16px;
}

.product-card__body p {
  margin: 0 0 8px;
  font-size: 13px;
  color: var(--color-ink-fainter);
}

.product-card__price {
  font-weight: 700;
  color: var(--color-accent);
}
</style>
