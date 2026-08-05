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
      // Placeholder so the cart page's checkout button has somewhere to go before Epic 9 builds
      // the real checkout flow (Checkout.dc.html: customer details, location pin, payment method).
      path: '/checkout',
      name: 'checkout',
      component: () => import('../views/CheckoutPlaceholderView.vue'),
    },
  ],
});
