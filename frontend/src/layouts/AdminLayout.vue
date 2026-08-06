<script setup lang="ts">
import { RouterLink, RouterView, useRouter } from 'vue-router';
import { useAuthStore } from '../stores/auth';

// No design mock covers the admin area (Epic 13/14, same as staff login/kitchen board) — reuses
// the existing design tokens rather than inventing a third visual language.
const auth = useAuthStore();
const router = useRouter();

async function logout() {
  await auth.logout();
  await router.replace('/staff/login');
}
</script>

<template>
  <div class="admin-shell">
    <nav class="admin-nav">
      <p class="admin-nav__brand">CROWD Admin</p>
      <RouterLink to="/admin/dashboard">Dashboard</RouterLink>
      <RouterLink to="/admin/menu">Menu</RouterLink>
      <RouterLink to="/admin/stock">Stock</RouterLink>
      <RouterLink to="/admin/orders">Orders</RouterLink>
      <RouterLink to="/admin/reconciliation">Reconciliation</RouterLink>
      <div class="admin-nav__spacer" />
      <span class="admin-nav__user">{{ auth.username }}</span>
      <button class="btn btn--outline btn--sm" @click="logout">Sign out</button>
    </nav>
    <main class="admin-content">
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
.admin-shell {
  min-height: 100vh;
  background: var(--color-bg);
  color: var(--color-ink);
}

.admin-nav {
  display: flex;
  align-items: center;
  gap: 20px;
  padding: 16px 24px;
  background: var(--color-white);
  border-bottom: 1px solid var(--color-border-soft);
  flex-wrap: wrap;
}

.admin-nav__brand {
  font-weight: 700;
  margin: 0;
  margin-right: 8px;
}

.admin-nav a {
  color: var(--color-ink-soft);
  font-weight: 600;
  font-size: 14px;
}

.admin-nav a.router-link-active {
  color: var(--color-accent);
}

.admin-nav__spacer {
  flex: 1;
}

.admin-nav__user {
  font-size: 14px;
  color: var(--color-ink-faint);
}

.admin-content {
  padding: 24px;
  max-width: 1200px;
  margin: 0 auto;
}
</style>
