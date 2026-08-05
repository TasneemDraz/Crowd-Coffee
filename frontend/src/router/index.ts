import { createRouter, createWebHistory } from 'vue-router';

export const router = createRouter({
  history: createWebHistory(),
  scrollBehavior(to) {
    if (to.hash) {
      return { el: to.hash, behavior: 'smooth' };
    }
    return { top: 0 };
  },
  routes: [
    {
      path: '/',
      name: 'landing',
      // Route-level code splitting (T6.4.3): the menu bundle (fetch/render logic for 161
      // products) never ships to a visitor who only loads the landing page.
      component: () => import('../views/LandingView.vue'),
    },
    {
      path: '/menu',
      name: 'menu',
      component: () => import('../views/MenuView.vue'),
    },
    {
      // Placeholder so the menu page's cart bar has somewhere to go before Epic 8 builds the
      // real cart page (Cart.dc.html: line items, qty steppers, delivery/pickup toggle).
      path: '/cart',
      name: 'cart',
      component: () => import('../views/CartPlaceholderView.vue'),
    },
  ],
});
