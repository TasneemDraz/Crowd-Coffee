<script setup lang="ts">
import { RouterLink } from 'vue-router';
</script>

<template>
  <div class="placeholder">
    <h1>Checkout</h1>
    <p>The real checkout flow (customer details, location pin, payment method) lands in Epic 9.</p>
    <RouterLink to="/cart" class="btn btn--dark">Back to Cart</RouterLink>
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
