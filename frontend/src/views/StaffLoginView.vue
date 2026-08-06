<script setup lang="ts">
import { ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useAuthStore } from '../stores/auth';
import { ApiError } from '../api/auth';

// No design mock covers the staff area (Epic 11/12 aren't in the CROWD design package, which
// only ever covered the customer-facing site) — this reuses the existing design tokens rather
// than inventing a second visual language for two internal-only screens.
const auth = useAuthStore();
const router = useRouter();
const route = useRoute();

const username = ref('');
const password = ref('');
const submitting = ref(false);
const errorMessage = ref('');

async function submit() {
  errorMessage.value = '';
  if (!username.value.trim() || !password.value) {
    errorMessage.value = 'Enter both a username and password.';
    return;
  }

  submitting.value = true;
  try {
    await auth.login(username.value.trim(), password.value);
    const redirectTo = typeof route.query.redirect === 'string' ? route.query.redirect : '/kitchen';
    await router.replace(redirectTo);
  } catch (err) {
    errorMessage.value = err instanceof ApiError ? err.message : 'Login failed — please try again.';
  } finally {
    submitting.value = false;
  }
}
</script>

<template>
  <div class="login-page">
    <form class="login-card" @submit.prevent="submit">
      <p class="eyebrow">Staff</p>
      <h1>Sign in</h1>

      <label class="field">
        <span>Username</span>
        <input v-model="username" type="text" autocomplete="username" :disabled="submitting" />
      </label>

      <label class="field">
        <span>Password</span>
        <input v-model="password" type="password" autocomplete="current-password" :disabled="submitting" />
      </label>

      <p v-if="errorMessage" class="error">{{ errorMessage }}</p>

      <button class="btn btn--accent" type="submit" :disabled="submitting">
        {{ submitting ? 'Signing in…' : 'Sign in' }}
      </button>
    </form>
  </div>
</template>

<style scoped>
.login-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--color-bg);
  color: var(--color-ink);
  padding: 24px;
}

.login-card {
  width: 100%;
  max-width: 360px;
  background: var(--color-white);
  border: 1px solid var(--color-border-soft);
  border-radius: var(--radius-lg);
  padding: 32px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.login-card h1 {
  margin: 0 0 8px;
  font-size: 28px;
}

.eyebrow {
  color: var(--color-accent);
}

.field {
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-size: 14px;
  font-weight: 600;
}

.field input {
  padding: 12px 14px;
  border-radius: var(--radius-sm);
  border: 1.5px solid var(--color-border-idle);
  font-size: 16px;
  font-family: inherit;
}

.field input:focus-visible {
  border-color: var(--color-accent);
}

.error {
  color: #b3261e;
  font-size: 14px;
  margin: 0;
}

.btn {
  margin-top: 8px;
  width: 100%;
}
</style>
