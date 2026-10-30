import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { configDefaults, defineConfig } from 'vitest/config'

// The API the dev server proxies to: its https launch profile, unless the Playwright smoke test starts one elsewhere
const apiOrigin = process.env.QUALITRACK_API_ORIGIN ?? 'https://localhost:7178'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    // Same-origin calls in development, so the session cookie is sent. The target is the API's https launch profile:
    // its cookies are Secure-only, and antiforgery refuses to issue or check a token on a plain-HTTP request.
    // secure: false accepts the ASP.NET Core development certificate without it being trusted by Node.
    proxy: {
      '/api': { target: apiOrigin, secure: false },
      '/health': { target: apiOrigin, secure: false },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    // The Playwright smoke test runs in a real browser against the real API (npm run e2e), not in jsdom
    exclude: [...configDefaults.exclude, 'e2e/**'],
    // See src/test/setup.ts: a page test that types into several fields can take a few seconds under load
    testTimeout: 15_000,
  },
})
