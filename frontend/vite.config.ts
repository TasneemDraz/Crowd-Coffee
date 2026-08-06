/// <reference types="vitest/config" />
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: {
      // Forwards to the ASP.NET Core API (server/src/CrowdCoffee.Api, "http" launch profile ->
      // port 5076) so the browser sees a same-origin request in dev — no CORS setup needed yet
      // (that's Epic 16).
      '/api': {
        target: 'http://localhost:5076',
        changeOrigin: true,
      },
      // Epic 12: SignalR's hub connection negotiates over HTTP then upgrades to a WebSocket —
      // ws: true is what makes Vite's proxy forward that upgrade instead of dropping it.
      '/hubs': {
        target: 'http://localhost:5076',
        changeOrigin: true,
        ws: true,
      },
      // Story 13.1: admin-uploaded product images are served by the API's UseStaticFiles()
      // straight from wwwroot/uploads — without this, <img src="/uploads/..."> 404s through the
      // Vite dev server, which has no idea that path belongs to the API.
      '/uploads': {
        target: 'http://localhost:5076',
        changeOrigin: true,
      },
    },
  },
  test: {
    // jsdom, not the vitest default node environment — the cart store persists to localStorage.
    environment: 'jsdom',
  },
})
