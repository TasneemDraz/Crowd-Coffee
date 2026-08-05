<script setup lang="ts"></script>

<template>
  <section id="story" class="story">
    <div class="story__image-wrap">
      <picture>
        <source srcset="/images/landing/story.webp" type="image/webp" />
        <img
          src="/images/landing/story.jpg"
          alt="Inside CROWD Coffee: the counter, espresso machine, and 'a space for every crowd' wall lettering"
          class="story__image"
          loading="lazy"
        />
      </picture>
    </div>
    <div class="story__copy">
      <div class="eyebrow" style="color: var(--color-accent)">Our Story</div>
      <h2 class="section-heading" style="margin-bottom: 24px">Three brothers.<br />One crowd.</h2>
      <p class="story__paragraph">
        CROWD started as a family bet between three brothers who grew up on this same corniche —
        watching ships roll in, chasing good coffee, and wishing this stretch of Port Said had a
        place to gather. So they built one.
      </p>
      <p class="story__paragraph" style="margin-bottom: 32px">
        Every cup is still pulled, poured, and served with that same energy: unfiltered, connected,
        and made for everyone who walks through the door.
      </p>
      <div class="story__tags">
        <span class="tag-pill" style="background: var(--color-accent-green-surface); color: var(--color-accent-green)">FAMILY OWNED</span>
        <span class="tag-pill" style="background: var(--color-accent-surface); color: var(--color-accent)">SPECIALTY GRADE</span>
      </div>
    </div>
  </section>
</template>

<style scoped>
.story {
  padding: 96px 32px;
  max-width: 1200px;
  margin: 0 auto;
  display: flex;
  gap: 64px;
  align-items: center;
  flex-wrap: wrap;
}

.story__image-wrap {
  flex: 1;
  min-width: 320px;
  border-radius: var(--radius-lg);
  overflow: hidden;
}

.story__image {
  width: 100%;
  height: clamp(220px, 55vw, 520px);
  object-fit: cover;
  display: block;
}

.story__copy {
  flex: 1;
  min-width: 320px;
}

.story__paragraph {
  font-size: 17px;
  line-height: 1.7;
  color: var(--color-ink-soft);
  margin: 0 0 20px;
}

.story__tags {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
}
</style>
