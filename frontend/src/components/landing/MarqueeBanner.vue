<script setup lang="ts">
const TEXT = 'SPECIALTY COFFEE ✦ CRAFTED BAKES ✦ PORT SAID ✦ EST. BY THREE BROTHERS ✦ THE GOOD KIND OF NOISE ✦ ';
</script>

<template>
  <div class="marquee">
    <div class="marquee__track">
      <span class="marquee__text">{{ TEXT }}</span>
      <span class="marquee__text" aria-hidden="true">{{ TEXT }}</span>
    </div>
  </div>
</template>

<style scoped>
.marquee {
  background: var(--color-ink);
  overflow: hidden;
  padding: 16px 0;
}

.marquee__track {
  display: flex;
  width: max-content;
  animation: marquee-scroll 28s linear infinite;
}

.marquee__text {
  white-space: nowrap;
  color: white;
  font-weight: 700;
  font-size: 20px;
  letter-spacing: 0.02em;
  padding-right: 40px;
}

@media (prefers-reduced-motion: reduce) {
  .marquee__track {
    animation: none;
  }
}
</style>
