<script setup lang="ts">
import { RouterLink } from 'vue-router';
</script>

<template>
  <header id="top" class="hero">
    <picture>
      <source srcset="/images/landing/hero.webp" type="image/webp" />
      <img
        src="/images/landing/hero.jpg"
        alt="CROWD Coffee storefront glowing at night on the Port Said corniche, with a cruise ship in the harbor"
        class="hero__image"
        fetchpriority="high"
      />
    </picture>
    <div class="hero__scrim" />
    <div class="hero__content">
      <div class="hero__badge">PORT SAID · NEW MAMSHA CORNICHE</div>
      <h1 class="hero__title">A space<br />for every<br />crowd.</h1>
      <p class="hero__lede">
        Speciality coffee and crafted bakes, brewed steps from the harbor. Pull up, watch the ships
        roll in, and get loud with good coffee.
      </p>
      <div class="hero__actions">
        <RouterLink to="/menu" class="btn btn--accent">Order Delivery</RouterLink>
        <a href="#visit" class="btn btn--outline hero__outline-btn">Get Directions</a>
      </div>
    </div>
  </header>
</template>

<style scoped>
.hero {
  position: relative;
  min-height: 92vh;
  display: flex;
  align-items: flex-end;
  overflow: hidden;
}

.hero__image {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  object-fit: cover;
  object-position: center 40%;
}

.hero__scrim {
  position: absolute;
  inset: 0;
  background: linear-gradient(
    180deg,
    color-mix(in oklch, var(--color-ink) 10%, transparent) 0%,
    color-mix(in oklch, var(--color-ink) 80%, transparent) 100%
  );
}

.hero__content {
  position: relative;
  z-index: 2;
  padding: 64px 32px 72px;
  max-width: 1100px;
}

.hero__badge {
  display: inline-block;
  background: var(--color-accent);
  color: white;
  padding: 6px 14px;
  border-radius: var(--radius-pill);
  font-size: 13px;
  font-weight: 600;
  letter-spacing: 0.04em;
  margin-bottom: 20px;
}

.hero__title {
  font-family: var(--font-display);
  color: white;
  font-size: clamp(34px, 10vw, 120px);
  line-height: 0.95;
  margin: 0 0 20px;
  text-shadow: 0 6px 24px rgba(0, 0, 0, 0.35);
}

.hero__lede {
  color: oklch(95% 0.01 75);
  font-size: clamp(16px, 2vw, 20px);
  max-width: 520px;
  margin: 0 0 32px;
  line-height: 1.5;
}

.hero__actions {
  display: flex;
  gap: 16px;
  flex-wrap: wrap;
}

.hero__outline-btn {
  color: white;
}
</style>
