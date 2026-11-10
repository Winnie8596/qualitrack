import AxeBuilder from '@axe-core/playwright'
import { expect, test, type Browser, type Locator, type Page } from '@playwright/test'

// Shared by the browser specs. Every spec runs against the real API and web app that playwright.config.ts starts.

export const demoPassword = process.env.E2E_DEMO_PASSWORD ?? ''

export type DemoRole = 'inspector' | 'investigator' | 'qamanager'

export function demoEmail(role: DemoRole): string {
  return `${role}@qualitrack.demo`
}

/** Skips the calling spec when no demo password is set, as on a machine without the database running. */
export function skipWithoutDemoUsers() {
  test.skip(!demoPassword, 'Set E2E_DEMO_PASSWORD (and E2E_CONNECTION_STRING) to run the browser tests')
}

/** A new browser context per role, as three people on three machines would have. */
export async function signedIn(browser: Browser, role: DemoRole): Promise<Page> {
  const page = await (await browser.newContext()).newPage()
  await page.goto('/login')
  await page.getByLabel('Email').fill(demoEmail(role))
  await page.getByLabel('Password').fill(demoPassword)
  await page.getByRole('button', { name: 'Sign in' }).click()
  await expect(page.getByRole('button', { name: 'Sign out' })).toBeVisible()
  return page
}

/** A date some days from now, as datetime-local's parts in the browser's own time zone. */
export function daysFromNow(days: number) {
  const due = new Date(Date.now() + days * 24 * 60 * 60 * 1000)
  const pad = (value: number) => String(value).padStart(2, '0')
  const year = String(due.getFullYear())
  const month = pad(due.getMonth() + 1)
  const day = pad(due.getDate())
  const hour = pad(due.getHours())
  const minute = pad(due.getMinutes())
  return { value: `${year}-${month}-${day}T${hour}:${minute}`, year, month, day, hour, minute }
}

export async function expectStatus(page: Page, status: string) {
  const details = page.getByRole('region', { name: 'Details' })
  await expect(details.getByText(status, { exact: true })).toBeVisible()
}

/** Raises an NCR through the form and returns its page's path. */
export async function raiseNcr(inspector: Page, title: string): Promise<string> {
  await inspector.goto('/ncrs/new')
  await inspector.getByLabel('Title').fill(title)
  await inspector.getByLabel('Description').fill('Sharp burr on the mating face.')
  await inspector.getByLabel('Location').fill('Line 3')
  await inspector.getByRole('radio', { name: 'Major' }).check()
  await inspector.getByRole('button', { name: 'Raise NCR' }).click()
  await expect(inspector.getByRole('heading', { level: 1, name: new RegExp(title) })).toBeVisible()
  return new URL(inspector.url()).pathname
}

// WCAG 2.2 A and AA, the level public-sector and most enterprise buyers ask for
const wcagTags = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa']

/** Fails with axe's own report (rule, impact, the offending elements) if the page breaks a WCAG A/AA rule. */
export async function expectNoAccessibilityViolations(page: Page) {
  const { violations } = await new AxeBuilder({ page }).withTags(wcagTags).analyze()
  const summary = violations.map(({ id, impact, help, nodes }) => ({
    id,
    impact,
    help,
    targets: nodes.map((node) => node.target.join(' ')),
  }))
  expect(summary, `axe violations on ${page.url()}`).toEqual([])
}

/**
 * Presses Tab until the target has focus, as a keyboard user would, and fails if it never does:
 * a control the Tab key cannot reach is one a keyboard user cannot use.
 */
export async function tabTo(page: Page, target: Locator, maxPresses = 80) {
  await expect(target).toBeVisible()
  for (let presses = 0; presses < maxPresses; presses++) {
    await page.keyboard.press('Tab')
    if (await target.evaluate((element) => element === element.ownerDocument.activeElement)) {
      return
    }
  }
  throw new Error(`Tab never reached ${target.toString()} in ${maxPresses} presses`)
}

/** Tabs to a text field and types into it. */
export async function tabAndType(page: Page, field: Locator, text: string) {
  await tabTo(page, field)
  await page.keyboard.type(text)
  await expect(field).toHaveValue(text)
}

/** Tabs to a button or link and presses Enter on it. */
export async function tabAndPress(page: Page, control: Locator) {
  await tabTo(page, control)
  await page.keyboard.press('Enter')
}
