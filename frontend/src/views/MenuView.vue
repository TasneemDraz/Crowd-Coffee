<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { RouterLink } from 'vue-router';
import { useCatalog } from '../composables/useCatalog';
import { useSimpleCart } from '../composables/useSimpleCart';
import type { SimpleCartLine } from '../composables/useSimpleCart';
import CategoryChips from '../components/menu/CategoryChips.vue';
import ProductCard from '../components/menu/ProductCard.vue';
import CartBar from '../components/menu/CartBar.vue';
import DrinkCustomizeModal from '../components/menu/DrinkCustomizeModal.vue';
import type { ProductDto } from '../types/catalog';

// §4a: only these category slugs get the customization modal; everything else (sandwiches,
// breakfast, bakery, sweetbakeries, pastries, sigdesserts, cheesecakes, salads) bypasses it and
// adds straight to cart at base price. Mirrors the design's own DRINK_CATS constant — the DB
// encodes the same fact structurally (only these categories have ProductModifierGroup rows via
// the Epic 2 seed), this list is just the fast client-side gate so Add doesn't need a network
// round-trip to decide whether to open the modal.
const DRINK_CATS = [
  'black', 'classic', 'iced', 'speciality', 'sigdrinks',
  'noncoffee', 'frappe', 'matcha', 'shakes', 'smoothies', 'mojito',
];

const { categories, products, isLoading, error, ensureLoaded } = useCatalog();
const { add, addLine, count, subtotal } = useSimpleCart();

const activeSlug = ref('');
const modalProduct = ref<ProductDto | null>(null);

onMounted(async () => {
  await ensureLoaded();
  if (!activeSlug.value && categories.value.length > 0) {
    activeSlug.value = categories.value[0].slug;
  }
});

const visibleProducts = computed(() => products.value.filter((p) => p.categorySlug === activeSlug.value));

const modalCategoryLabel = computed(
  () => categories.value.find((c) => c.slug === modalProduct.value?.categorySlug)?.label ?? '',
);

function handleAdd(product: ProductDto) {
  if (DRINK_CATS.includes(product.categorySlug)) {
    modalProduct.value = product;
  } else {
    add(product);
  }
}

function handleModalConfirm(line: SimpleCartLine) {
  addLine(line);
  modalProduct.value = null;
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

    <DrinkCustomizeModal
      v-if="modalProduct"
      :product="modalProduct"
      :category-label="modalCategoryLabel"
      @close="modalProduct = null"
      @confirm="handleModalConfirm"
    />
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
