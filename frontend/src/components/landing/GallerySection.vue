<script setup lang="ts">
interface GalleryImage {
  name: string;
  alt: string;
  caption: string;
}

// Order matches the source design's gallery grid exactly.
const IMAGES: GalleryImage[] = [
  { name: 'gallery-1', alt: 'CROWD Coffee sign glowing at dusk with a cruise ship docked behind the harbor fence', caption: 'Golden hour, every hour' },
  { name: 'gallery-2', alt: 'Guests sitting outside CROWD Coffee at night with a cruise ship in the background', caption: 'Terrace nights on the corniche' },
  { name: 'gallery-3', alt: 'Barista in a green CROWD hoodie pouring a hand-drip coffee', caption: 'Crafted, cup by cup' },
  { name: 'gallery-4', alt: 'View of the Port Said harbor gates and corniche from the CROWD Coffee terrace', caption: 'Steps from the harbor gates' },
  { name: 'gallery-5', alt: 'CROWD Coffee storefront by day with guests seated outside', caption: 'Grab a seat outside' },
  { name: 'gallery-6', alt: 'CROWD lettering sign lit up at night above the storefront, cruise ship in the distance', caption: 'The sign that guides you in' },
  { name: 'gallery-7', alt: 'Cozy interior lounge seating with a curved mirror and a view of the Port Said sign outside', caption: 'A quieter corner inside' },
];
</script>

<template>
  <section id="gallery" class="gallery">
    <div class="gallery__heading">
      <div class="eyebrow" style="color: var(--color-accent-green)">On the Corniche</div>
      <h2 class="section-heading">Where the coffee meets the harbor</h2>
    </div>
    <div class="gallery__grid">
      <figure v-for="image in IMAGES" :key="image.name" class="gallery__tile">
        <picture>
          <source :srcset="`/images/landing/${image.name}.webp`" type="image/webp" />
          <img :src="`/images/landing/${image.name}.jpg`" :alt="image.alt" loading="lazy" />
        </picture>
        <figcaption>{{ image.caption }}</figcaption>
      </figure>
    </div>
  </section>
</template>

<style scoped>
.gallery {
  padding: 96px 32px;
  max-width: 1200px;
  margin: 0 auto;
}

.gallery__heading {
  text-align: center;
  margin-bottom: 56px;
}

.gallery__grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
  gap: 20px;
}

.gallery__tile {
  position: relative;
  border-radius: var(--radius-sm);
  overflow: hidden;
  height: 280px;
  margin: 0;
}

.gallery__tile img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}

.gallery__tile figcaption {
  position: absolute;
  left: 0;
  right: 0;
  bottom: 0;
  padding: 16px;
  background: linear-gradient(180deg, transparent, rgba(0, 0, 0, 0.65));
  color: white;
  font-size: 14px;
  font-weight: 600;
}
</style>
