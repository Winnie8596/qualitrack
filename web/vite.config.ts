import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    // Same-origin calls in development, so the session cookie is sent. The target is the API's https launch profile:
    // its cookies are Secure-only, and antiforgery refuses to issue or check a token on a plain-HTTP request.
    // secure: false accepts the ASP.NET Core development certificate without it being trusted by Node.
    proxy: {
      '/api': { target: 'https://localhost:7178', secure: false },
      '/health': { target: 'https://localhost:7178', secure: false },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
  },
})
