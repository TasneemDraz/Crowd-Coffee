import { defineStore } from 'pinia';
import { login as apiLogin, logout as apiLogout, refresh as apiRefresh, ApiError } from '../api/auth';
import type { StaffRole } from '../types/auth';

/**
 * Access token lives only in memory (this store's state), never localStorage — an XSS payload
 * that can read localStorage can read a token there, but one that only runs JS still can't reach
 * an httpOnly cookie, and a page reload loses this store's state anyway so there's nothing extra
 * to steal by keeping it in-memory. The refresh token (Story 11.2's actual persistence) is the
 * httpOnly cookie the browser manages entirely on its own; this store never sees its raw value.
 */
export const useAuthStore = defineStore('auth', {
  state: () => ({
    accessToken: null as string | null,
    expiresAt: null as number | null, // epoch ms
    username: null as string | null,
    role: null as StaffRole | null,
    restoring: true, // true until the initial silent-refresh attempt (tryRestoreSession) settles
  }),
  getters: {
    isAuthenticated: (state) => state.accessToken !== null,
    isManager: (state) => state.role === 'Manager',
  },
  actions: {
    applyAuthResponse(auth: { accessToken: string; expiresAt: string; username: string; role: StaffRole }) {
      this.accessToken = auth.accessToken;
      this.expiresAt = new Date(auth.expiresAt).getTime();
      this.username = auth.username;
      this.role = auth.role;
    },
    async login(username: string, password: string) {
      const auth = await apiLogin(username, password);
      this.applyAuthResponse(auth);
    },
    async logout() {
      await apiLogout().catch(() => {
        // Best-effort server-side revoke — the client-side state clear below is what actually
        // matters for "am I logged out" from the UI's perspective.
      });
      this.accessToken = null;
      this.expiresAt = null;
      this.username = null;
      this.role = null;
    },
    /** Called once on app load: exchanges the httpOnly refresh cookie (if any/still valid) for a
     * fresh access token, so a reload doesn't force a re-login. Failure just means "not logged
     * in" — not an error worth surfacing. */
    async tryRestoreSession() {
      try {
        const auth = await apiRefresh();
        this.applyAuthResponse(auth);
      } catch {
        this.accessToken = null;
        this.expiresAt = null;
      } finally {
        this.restoring = false;
      }
    },
    /** Returns a live access token, transparently refreshing first if the current one is at or
     * near expiry — call sites (kitchen board API calls, SignalR's accessTokenFactory) don't need
     * their own expiry-tracking logic. */
    async getValidAccessToken(): Promise<string | null> {
      const expiresSoon = this.expiresAt === null || this.expiresAt - Date.now() < 10_000;
      if (this.accessToken && !expiresSoon) {
        return this.accessToken;
      }

      try {
        const auth = await apiRefresh();
        this.applyAuthResponse(auth);
        return this.accessToken;
      } catch (err) {
        this.accessToken = null;
        this.expiresAt = null;
        if (err instanceof ApiError) {
          return null;
        }
        throw err;
      }
    },
  },
});
