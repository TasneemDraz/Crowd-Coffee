<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { RouterLink } from 'vue-router';
import { useCatalog } from '../composables/useCatalog';
import { useSimpleCart } from '../composables/useSimpleCart';
import CategoryChips from '../components/menu/CategoryChips.vue';
import ProductCard from '../components/menu/ProductCard.vue';
import CartBar from '../components/menu/CartBar.vue';
import type { ProductDto } from '../types/catalog';

const { categories, products, isLoading, error, ensureLoaded } = useCatalog();
const { add, count, subtotal } = useSimpleCart();

const activeSlug = ref('');

onMounted(async () => {
  await ensureLoaded();
  if (!activeSlug.value && categories.value.length > 0) {
    activeSlug.value = categories.value[0].slug;
  }
});

const visibleProducts = computed(() => products.value.filter((p) => p.categorySlug === activeSlug.value));

function handleAdd(product: ProductDto) {
  // TODO(Epic 7): drink categories (DRINK_CATS) should open the customization modal instead of
  // adding directly — every category bypasses it for now since the modal doesn't exist yet.
  add(product);
}
</script>

<template>
  <div class="menu-page" :style="{ paddingBottom: count > 0 ? '110px' : '40px' }">
    <nav class="menu-nav">
      <RouterLink to="/" class="menu-nav__brand">CROWD</RouterLink>
      <div class="menu-nav__actions">
        <RouterLink to="/cart" class="btn btn--dark btn--sm">Cart · {{ count }}</RouterLink>
        <RouterLink to="/" class="menu-nav__home">Home →</RouterLink>
      </div>
    </nav>

    <div class="menu-heading">
      <div class="eyebrow" style="color: var(--color-accent)">New Mamsha · Port Said</div>
      <h1 class="menu-heading__title">The Menu</h1>
    </div>

    <p v-if="error" class="menu-status menu-status--error">{{ error }}</p>
    <p v-else-if="isLoading" class="menu-status">Loading the menu…</p>

    <template v-else>
      <CategoryChips :categories="categories" :active-slug="activeSlug" @select="activeSlug = $event" />

      <div class="menu-list">
        <ProductCard v-for="product in visibleProducts" :key="product.id" :product="product" @add="handleAdd" />
      </div>
    </template>

    <CartBar :count="count" :subtotal="subtotal" />
  </div>
</template>

<style scoped>
.menu-page {
  background: var(--color-bg);
  color: var(--color-ink);
  min-height: 100vh;
}

.menu-nav {
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

.menu-nav__brand {
  font-family: var(--font-display);
  font-size: 24px;
  color: var(--color-ink);
}

.menu-nav__actions {
  display: flex;
  gap: 12px;
  align-items: center;
}

.menu-nav__home {
  font-weight: 600;
  font-size: 13px;
  color: var(--color-ink);
}

.menu-heading {
  padding: 28px 20px 8px;
  max-width: 900px;
  margin: 0 auto;
}

.menu-heading__title {
  font-family: var(--font-display);
  font-size: clamp(28px, 6vw, 48px);
  margin: 0;
}

.menu-status {
  padding: 24px 20px;
  max-width: 900px;
  margin: 0 auto;
  color: var(--color-ink-faint);
}

.menu-status--error {
  color: var(--color-accent);
}

.menu-list {
  padding: 24px 20px;
  max-width: 900px;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  gap: 12px;
}
</style>
