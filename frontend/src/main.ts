import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';
import { router } from './router';
import { useAuthStore } from './stores/auth';
import './styles/global.css';

const app = createApp(App);
app.use(createPinia());
app.use(router);

// Exchange the httpOnly refresh cookie (if any) for a fresh access token before the first route
// resolves — the /kitchen guard checks isAuthenticated synchronously, so a staff member with a
// still-valid session from an earlier visit must already be restored by the time it runs.
useAuthStore()
  .tryRestoreSession()
  .finally(() => app.mount('#app'));
