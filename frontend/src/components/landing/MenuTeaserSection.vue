<script setup lang="ts">
import { RouterLink } from 'vue-router';
</script>

<template>
  <section id="menu" class="menu-teaser">
    <div class="menu-teaser__inner">
      <div class="menu-teaser__heading">
        <div class="eyebrow" style="color: var(--color-accent)">What's Brewing</div>
        <h2 class="section-heading">The menu, in short</h2>
      </div>

      <div class="menu-teaser__grid">
        <div class="teaser-card teaser-card--photo">
          <div class="teaser-card__photo">
            <picture>
              <source srcset="/images/landing/menu-teaser-specialty.webp" type="image/webp" />
              <img
                src="/images/landing/menu-teaser-specialty.jpg"
                alt="Latte art in an orange CROWD cup beside the espresso machine"
                loading="lazy"
              />
            </picture>
          </div>
          <div class="teaser-card__body">
            <h3>Specialty Coffee</h3>
            <p>Pour-overs, espresso, and single-origin drips pulled by baristas who take it seriously.</p>
          </div>
        </div>

        <div class="teaser-card teaser-card--solid" style="background: var(--color-accent)">
          <h3>Iced &amp; Cold Brew</h3>
          <p>Iced lattes, cold brew, frappés and mojitos for hot corniche afternoons.</p>
        </div>

        <div class="teaser-card teaser-card--solid" style="background: var(--color-accent-green)">
          <h3>Cheesecakes</h3>
          <p>San Sebastian, pistachio, biscoff lotus, and ten more reasons to stay for dessert.</p>
        </div>

        <div class="teaser-card teaser-card--solid" style="background: oklch(28% 0.02 50)">
          <h3>Matcha &amp; More</h3>
          <p>Matcha lattes, fresh juices and shakes for the rest of the crowd.</p>
        </div>
      </div>

      <div class="menu-teaser__cta">
        <RouterLink to="/menu" class="btn btn--dark">See Full Menu &amp; Order →</RouterLink>
      </div>
    </div>
  </section>
</template>

<style scoped>
.menu-teaser {
  padding: 96px 32px;
  background: var(--color-bg-alt);
}

.menu-teaser__inner {
  max-width: 1200px;
  margin: 0 auto;
}

.menu-teaser__heading {
  text-align: center;
  margin-bottom: 56px;
}

.menu-teaser__grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
  gap: 24px;
}

.teaser-card {
  border-radius: var(--radius-md);
}

.teaser-card--photo {
  overflow: hidden;
  background: white;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.06);
}

.teaser-card__photo {
  height: 220px;
  overflow: hidden;
}

.teaser-card__photo img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}

.teaser-card__body {
  padding: 20px;
}

.teaser-card__body h3 {
  margin: 0 0 8px;
  font-size: 20px;
}

.teaser-card__body p {
  margin: 0;
  color: var(--color-ink-faint);
  font-size: 14px;
  line-height: 1.5;
}

.teaser-card--solid {
  color: white;
  padding: 32px 20px;
  display: flex;
  flex-direction: column;
  justify-content: flex-end;
  min-height: 284px;
}

.teaser-card--solid h3 {
  margin: 0 0 8px;
  font-size: 20px;
}

.teaser-card--solid p {
  margin: 0;
  font-size: 14px;
  line-height: 1.5;
  opacity: 0.92;
}

.menu-teaser__cta {
  text-align: center;
  margin-top: 48px;
}
</style>
