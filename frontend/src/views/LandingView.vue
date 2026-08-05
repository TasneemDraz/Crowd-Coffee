<script setup lang="ts">
import SiteNav from '../components/landing/SiteNav.vue';
import HeroSection from '../components/landing/HeroSection.vue';
import MarqueeBanner from '../components/landing/MarqueeBanner.vue';
import StorySection from '../components/landing/StorySection.vue';
import MenuTeaserSection from '../components/landing/MenuTeaserSection.vue';
import GallerySection from '../components/landing/GallerySection.vue';
import VisitSection from '../components/landing/VisitSection.vue';
import AppFooter from '../components/landing/AppFooter.vue';
</script>

<template>
  <div class="landing">
    <SiteNav />
    <HeroSection />
    <MarqueeBanner />
    <StorySection />
    <MenuTeaserSection />
    <GallerySection />
    <VisitSection />
    <AppFooter />
  </div>
</template>

<style scoped>
.landing {
  background: var(--color-bg);
  color: var(--color-ink);
}
</style>
