/// <reference types="vitest/config" />
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: {
      // Forwards to the ASP.NET Core API (server/src/CrowdCoffee.Api, default port 5000) so the
      // browser sees a same-origin request in dev — no CORS setup needed yet (that's Epic 16).
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
  test: {
    // jsdom, not the vitest default node environment — the cart store persists to localStorage.
    environment: 'jsdom',
  },
})
