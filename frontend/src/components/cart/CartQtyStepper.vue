<script setup lang="ts">
defineProps<{ qty: number }>();
const emit = defineEmits<{ increase: []; decrease: [] }>();
</script>

<template>
  <div class="qty-stepper">
    <button type="button" aria-label="Decrease quantity" @click="emit('decrease')">−</button>
    <span>{{ qty }}</span>
    <button type="button" aria-label="Increase quantity" @click="emit('increase')">+</button>
  </div>
</template>

<style scoped>
.qty-stepper {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.qty-stepper button {
  width: 30px;
  height: 30px;
  border-radius: 50%;
  border: 1px solid var(--color-border-idle);
  background: white;
  font-weight: 700;
  cursor: pointer;
  flex-shrink: 0;
}

.qty-stepper span {
  min-width: 18px;
  text-align: center;
  font-weight: 600;
}
</style>
