import { defineConfig } from '@playwright/test'

// Lighthouse audits (plan.md Phase 5), run with `npm run lighthouse`. Unlike the smoke tests, the web app is the
// production build served by `vite preview` (which proxies /api like the dev server does): scoring the dev
// server would measure unbundled, unminified modules no user ever downloads. Same environment variables as
// playwright.config.ts: E2E_CONNECTION_STRING and E2E_DEMO_PASSWORD.
const connectionString = process.env.E2E_CONNECTION_STRING ?? ''
const demoPassword = process.env.E2E_DEMO_PASSWORD ?? ''

const apiOrigin = 'https://localhost:7298'
const webOrigin = 'http://localhost:5298'

export default defineConfig({
  testDir: './lighthouse',
  fullyParallel: false,
  workers: 1,
  forbidOnly: Boolean(process.env.CI),
  reporter: 'list',
  // A Lighthouse run takes 10–20 seconds a page, and the audit covers every page
  timeout: 600_000,
  use: { baseURL: webOrigin },
  webServer: [
    {
      command: `dotnet run --project ../src/QualiTrack.Api --launch-profile https --urls ${apiOrigin}`,
      url: `${apiOrigin}/health`,
      ignoreHTTPSErrors: true,
      reuseExistingServer: false,
      timeout: 180_000,
      env: {
        ConnectionStrings__QualiTrack: connectionString,
        DemoUsers__Inspector: demoPassword,
        DemoUsers__Investigator: demoPassword,
        DemoUsers__QaManager: demoPassword,
        // Every spec signs in afresh from the one address, far past the 5-a-minute guard against password
        // guessing; the API's integration tests check that limit itself
        LoginRateLimit__PermitLimit: '1000',
      },
    },
    {
      command: 'npm run build && npm run preview -- --port 5298 --strictPort',
      url: webOrigin,
      reuseExistingServer: false,
      timeout: 180_000,
      env: { QUALITRACK_API_ORIGIN: apiOrigin },
    },
  ],
})
