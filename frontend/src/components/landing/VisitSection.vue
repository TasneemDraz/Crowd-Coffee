<script setup lang="ts">
const PHONE_NUMBER = '01070091239';
const INSTAGRAM_URL = 'https://www.instagram.com/crowd_specialtycoffee/';
</script>

<template>
  <section id="visit" class="visit">
    <div class="visit__inner">
      <div class="visit__image-wrap">
        <picture>
          <source srcset="/images/landing/visit.webp" type="image/webp" />
          <img
            src="/images/landing/visit.jpg"
            alt="A cup of coffee on a table with a cruise ship passing behind CROWD Coffee"
            class="visit__image"
            loading="lazy"
          />
        </picture>
      </div>
      <div class="visit__copy">
        <div class="eyebrow" style="color: var(--color-accent-terracotta-light)">Visit</div>
        <h2 class="section-heading" style="margin-bottom: 24px">Come sit with the crowd</h2>
        <p class="visit__line">New Mamsha Corniche, Port Said, Egypt</p>
        <p class="visit__line" style="margin-bottom: 32px">
          Right on the water — you'll spot the ships before you spot the sign.
        </p>
        <div class="visit__actions">
          <a :href="`tel:${PHONE_NUMBER}`" class="btn btn--white">Call: {{ PHONE_NUMBER }}</a>
          <a :href="INSTAGRAM_URL" target="_blank" rel="noopener" class="btn btn--outline visit__instagram">
            @crowd_specialtycoffee
          </a>
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
.visit {
  padding: 96px 32px;
  background: var(--color-ink);
  color: white;
}

.visit__inner {
  max-width: 1200px;
  margin: 0 auto;
  display: flex;
  gap: 64px;
  flex-wrap: wrap;
  align-items: center;
}

.visit__image-wrap {
  flex: 1;
  min-width: 300px;
  border-radius: var(--radius-lg);
  overflow: hidden;
}

.visit__image {
  width: 100%;
  height: clamp(220px, 50vw, 420px);
  object-fit: cover;
  display: block;
}

.visit__copy {
  flex: 1;
  min-width: 300px;
}

.visit__line {
  font-size: 17px;
  line-height: 1.7;
  margin: 0 0 8px;
  opacity: 0.85;
}

.visit__actions {
  display: flex;
  gap: 16px;
  flex-wrap: wrap;
}

.visit__instagram {
  color: white;
}
</style>
