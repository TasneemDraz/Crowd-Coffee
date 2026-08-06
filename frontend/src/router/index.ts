import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from '../stores/auth';

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
    {
      path: '/staff/login',
      name: 'staff-login',
      component: () => import('../views/StaffLoginView.vue'),
    },
    {
      // Epic 12: three-column live order board. Staff-only (either role — a barista runs the
      // board day to day, a manager can too), guarded below.
      path: '/kitchen',
      name: 'kitchen',
      component: () => import('../views/KitchenBoardView.vue'),
      meta: { requiresStaff: true },
    },
    {
      // Epic 13/14: menu/stock/order/dashboard management — manager-only (children inherit this
      // meta; vue-router doesn't require repeating it per child).
      path: '/admin',
      component: () => import('../layouts/AdminLayout.vue'),
      meta: { requiresManager: true },
      children: [
        { path: '', redirect: '/admin/dashboard' },
        { path: 'dashboard', name: 'admin-dashboard', component: () => import('../views/AdminDashboardView.vue') },
        { path: 'menu', name: 'admin-menu', component: () => import('../views/AdminMenuView.vue') },
        { path: 'stock', name: 'admin-stock', component: () => import('../views/AdminStockView.vue') },
        { path: 'orders', name: 'admin-orders', component: () => import('../views/AdminOrdersView.vue') },
        { path: 'reconciliation', name: 'admin-reconciliation', component: () => import('../views/AdminReconciliationView.vue') },
      ],
    },
  ],
});

router.beforeEach(async (to) => {
  const auth = useAuthStore();

  // vue-router starts resolving the initial route as soon as the router is installed
  // (app.use(router) in main.ts), which fires before main.ts's own restoration-then-mount
  // sequence finishes — without this, a page reload on a guarded route would see
  // isAuthenticated still false (the restore fetch hasn't resolved yet) and bounce to login even
  // though the refresh cookie is perfectly valid. tryRestoreSession() is memoized, so this is a
  // no-op await once restoration has already completed.
  if ((to.meta.requiresStaff || to.meta.requiresManager) && auth.restoring) {
    await auth.tryRestoreSession();
  }

  if (to.meta.requiresManager) {
    if (!auth.isAuthenticated) {
      return { path: '/staff/login', query: { redirect: to.fullPath } };
    }
    if (!auth.isManager) {
      // A logged-in barista just isn't a manager — send them to the screen their role is
      // actually for, not back to a login form for a session they already have.
      return { path: '/kitchen' };
    }
    return true;
  }

  if (to.meta.requiresStaff && !auth.isAuthenticated) {
    return { path: '/staff/login', query: { redirect: to.fullPath } };
  }

  return true;
});
