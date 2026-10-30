import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    // Same-origin calls in development, so the session cookie is sent; the API's http launch profile listens on 5097.
    proxy: {
      '/api': 'http://localhost:5097',
      '/health': 'http://localhost:5097',
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
  },
})
