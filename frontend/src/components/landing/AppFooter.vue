<script setup lang="ts">
const INSTAGRAM_URL = 'https://www.instagram.com/crowd_specialtycoffee/';
</script>

<template>
  <footer class="footer">
    <div class="footer__brand">CROWD</div>
    <div class="footer__links">
      <a href="#story">Story</a>
      <a href="#menu">Menu</a>
      <a href="#gallery">Gallery</a>
      <a href="#visit">Visit</a>
      <a :href="INSTAGRAM_URL" target="_blank" rel="noopener">Instagram</a>
    </div>
    <div class="footer__copyright">© CROWD Speciality Coffee — Port Said</div>
  </footer>
</template>

<style scoped>
.footer {
  padding: 48px 32px;
  background: var(--color-bg);
  border-top: 1px solid var(--color-border-soft);
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 20px;
}

.footer__brand {
  font-family: var(--font-display);
  font-size: 24px;
}

.footer__links {
  display: flex;
  gap: 24px;
  flex-wrap: wrap;
  font-size: 14px;
}

.footer__copyright {
  font-size: 13px;
  color: var(--color-ink-fainter);
}
</style>
