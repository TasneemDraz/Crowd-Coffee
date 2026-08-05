<script setup lang="ts">
import { RouterLink } from 'vue-router';
</script>

<template>
  <nav class="nav">
    <a href="#top" class="nav__brand">CROWD</a>
    <div class="nav__links">
      <a href="#story">Story</a>
      <a href="#menu">Menu</a>
      <a href="#gallery">Gallery</a>
      <a href="#visit">Visit</a>
      <RouterLink to="/menu" class="btn btn--accent btn--sm">Order</RouterLink>
    </div>
  </nav>
</template>

<style scoped>
.nav {
  position: sticky;
  top: 0;
  z-index: 50;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 24px;
  padding: 16px 32px;
  background: color-mix(in oklch, var(--color-bg) 92%, transparent);
  backdrop-filter: blur(8px);
  border-bottom: 1px solid var(--color-border-soft);
  flex-wrap: wrap;
}

.nav__brand {
  font-family: var(--font-display);
  font-size: 28px;
  color: var(--color-ink);
}

.nav__links {
  display: flex;
  gap: 28px;
  align-items: center;
  flex-wrap: wrap;
  font-size: 15px;
  font-weight: 500;
}

.nav__links > a:not(.btn) {
  color: var(--color-ink);
}
.nav__links > a:not(.btn):hover {
  color: var(--color-accent);
}
</style>
