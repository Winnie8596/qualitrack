import { defineConfig, devices } from '@playwright/test'

// The browser smoke test (plan.md S5): the real API and web app, started here, against a PostgreSQL that is
// already running (docker compose locally, a service container in CI). E2E_CONNECTION_STRING points at it;
// E2E_DEMO_PASSWORD is every demo user's password, which the API seeds on first start in Development.
const connectionString = process.env.E2E_CONNECTION_STRING ?? ''
const demoPassword = process.env.E2E_DEMO_PASSWORD ?? ''
const isCi = Boolean(process.env.CI)

// Ports of their own, never reused: a dev server already on the usual ports may be another app entirely
const apiOrigin = 'https://localhost:7299'
const webOrigin = 'http://localhost:5299'

export default defineConfig({
  testDir: './e2e',
  // One lifecycle, step by step: the steps share an NCR, so they cannot run in parallel
  fullyParallel: false,
  workers: 1,
  forbidOnly: isCi,
  retries: isCi ? 1 : 0,
  reporter: isCi ? [['github'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL: webOrigin,
    trace: 'retain-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
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
      },
    },
    {
      command: 'npm run dev -- --port 5299 --strictPort',
      url: webOrigin,
      reuseExistingServer: false,
      env: { QUALITRACK_API_ORIGIN: apiOrigin },
    },
  ],
})
