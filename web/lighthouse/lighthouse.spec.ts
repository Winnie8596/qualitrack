import { chromium, expect, test, type Page } from '@playwright/test'
import lighthouse from 'lighthouse'
import { mkdir, writeFile } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import path from 'node:path'
import { demoEmail, demoPassword, raiseNcr, skipWithoutDemoUsers, type DemoRole } from '../e2e/support.ts'

// Lighthouse on every page, as the role that uses it (plan.md Phase 5): accessibility ≥ 90 everywhere, and
// performance ≥ 90 on the NCR list. Lighthouse's default settings are kept, which emulate a mid-range phone
// on a slow 4G connection, the harder of its two profiles. The HTML reports land in lighthouse-report/.

const minimumScore = 90
const debuggingPort = 9333
const reportDirectory = path.resolve(import.meta.dirname, '..', 'lighthouse-report')

type Audit = { name: string; path: string; checkPerformance?: boolean }

// Lighthouse drives Chrome through its debugging port, in the profile Playwright signed in with, so it opens
// each page with that role's session cookie. A persistent context is that default profile.
async function launchBrowser() {
  const profile = path.join(tmpdir(), `qualitrack-lighthouse-${Date.now()}`)
  return chromium.launchPersistentContext(profile, {
    baseURL: test.info().project.use.baseURL,
    // The full Chromium in its new headless mode: the slimmer headless shell lacks what Lighthouse drives
    channel: 'chromium',
    args: [`--remote-debugging-port=${debuggingPort}`],
  })
}

async function signIn(page: Page, role: DemoRole) {
  await page.goto('/login')
  await page.getByLabel('Email').fill(demoEmail(role))
  await page.getByLabel('Password').fill(demoPassword)
  await page.getByRole('button', { name: 'Sign in' }).click()
  await expect(page.getByRole('button', { name: 'Sign out' })).toBeVisible()
}

async function signOut(page: Page) {
  await page.getByRole('button', { name: 'Sign out' }).click()
  await expect(page.getByRole('heading', { level: 1, name: 'Sign in' })).toBeVisible()
}

type Scores = { accessibility: number; performance: number }

async function audit(page: Page, { name, path: pagePath }: Audit): Promise<Scores> {
  const url = new URL(pagePath, page.url()).toString()
  const result = await lighthouse(url, {
    port: debuggingPort,
    output: 'html',
    logLevel: 'error',
    onlyCategories: ['accessibility', 'performance'],
    // Keep the session cookie Playwright signed in with
    disableStorageReset: true,
  })
  if (!result) {
    throw new Error(`Lighthouse returned no result for ${url}`)
  }
  // A lost session would score the sign-in page in the real page's place, and pass
  expect(new URL(result.lhr.finalDisplayedUrl).pathname, `${name}: the page Lighthouse ended on`).toBe(pagePath)
  const fileName = `${name.toLowerCase().replaceAll(/[^a-z0-9]+/g, '-')}.html`
  await writeFile(path.join(reportDirectory, fileName), String(result.report))
  const score = (category: 'accessibility' | 'performance') =>
    Math.round((result.lhr.categories[category]?.score ?? 0) * 100)
  return { accessibility: score('accessibility'), performance: score('performance') }
}

test('every page scores at least 90 for accessibility, and the NCR list for performance', async () => {
  skipWithoutDemoUsers()
  await mkdir(reportDirectory, { recursive: true })
  const browser = await launchBrowser()
  const page = browser.pages()[0] ?? (await browser.newPage())
  const results: (Audit & Scores)[] = []
  const run = async (pages: Audit[]) => {
    for (const each of pages) {
      results.push({ ...each, ...(await audit(page, each)) })
    }
  }

  await page.goto('/login')
  await run([
    { name: 'Sign in', path: '/login' },
    { name: 'Page not found', path: '/no-such-page' },
  ])

  await signIn(page, 'inspector')
  const ncrPath = await raiseNcr(page, `Lighthouse check ${Date.now()}`)
  await run([
    { name: 'Home', path: '/' },
    { name: 'Raise an NCR', path: '/ncrs/new' },
    { name: 'NCR detail (inspector)', path: ncrPath },
  ])
  await signOut(page)

  await signIn(page, 'qamanager')
  await run([
    { name: 'NCR list', path: '/ncrs', checkPerformance: true },
    { name: 'NCR detail (QA manager)', path: ncrPath },
    { name: 'Dashboard', path: '/dashboard' },
  ])
  await browser.close()

  console.table(results.map(({ name, accessibility, performance }) => ({ page: name, accessibility, performance })))
  for (const { name, accessibility, performance, checkPerformance } of results) {
    expect.soft(accessibility, `${name}: accessibility`).toBeGreaterThanOrEqual(minimumScore)
    if (checkPerformance) {
      expect.soft(performance, `${name}: performance`).toBeGreaterThanOrEqual(minimumScore)
    }
  }
})
