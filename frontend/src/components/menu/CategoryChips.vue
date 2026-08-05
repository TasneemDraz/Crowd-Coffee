<script setup lang="ts">
import type { CategoryDto } from '../../types/catalog';

defineProps<{
  categories: CategoryDto[];
  activeSlug: string;
}>();

const emit = defineEmits<{ select: [slug: string] }>();
</script>

<template>
  <div class="chips" role="tablist" aria-label="Menu categories">
    <button
      v-for="category in categories"
      :key="category.slug"
      type="button"
      role="tab"
      class="chip"
      :class="{ 'chip--active': category.slug === activeSlug }"
      :aria-selected="category.slug === activeSlug"
      @click="emit('select', category.slug)"
    >
      {{ category.label }}
    </button>
  </div>
</template>

<style scoped>
.chips {
  position: sticky;
  top: 57px;
  z-index: 40;
  background: var(--color-bg);
  padding: 12px 20px;
  display: flex;
  gap: 8px;
  overflow-x: auto;
  border-bottom: 1px solid var(--color-border-soft);
}

.chip {
  background: transparent;
  color: var(--color-ink);
  border: 1px solid var(--color-border-idle);
  padding: 10px 18px;
  border-radius: var(--radius-pill);
  font-weight: 600;
  font-size: 14px;
  white-space: nowrap;
  flex-shrink: 0;
  cursor: pointer;
}

.chip--active {
  background: var(--color-accent-green);
  color: white;
  border-color: var(--color-accent-green);
}
</style>
