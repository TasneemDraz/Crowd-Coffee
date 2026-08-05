<script setup lang="ts">
import { RouterLink } from 'vue-router';
</script>

<template>
  <div class="placeholder">
    <h1>Cart</h1>
    <p>The real cart page (line items, qty steppers, delivery/pickup toggle) lands in Epic 8.</p>
    <RouterLink to="/menu" class="btn btn--dark">Back to Menu</RouterLink>
  </div>
</template>

<style scoped>
.placeholder {
  max-width: 480px;
  margin: 96px auto;
  padding: 0 24px;
  text-align: center;
}
</style>
