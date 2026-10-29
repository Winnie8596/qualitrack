import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // Same-origin calls in development; the API's http launch profile listens on 5097.
    proxy: {
      '/health': 'http://localhost:5097',
    },
  },
})
