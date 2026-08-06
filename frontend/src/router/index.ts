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
      path: '/cart',
      name: 'cart',
      component: () => import('../views/CartView.vue'),
    },
    {
      path: '/checkout',
      name: 'checkout',
      component: () => import('../views/CheckoutView.vue'),
    },
    {
      // Where the payment gateway's hosted page redirects the browser after the customer pays
      // or cancels (Epic 10.2) — configured as the integration's "transaction redirection URL"
      // in the Paymob dashboard, not something this app can set per-request.
      path: '/checkout/return',
      name: 'checkout-return',
      component: () => import('../views/CheckoutReturnView.vue'),
    },
  ],
});
