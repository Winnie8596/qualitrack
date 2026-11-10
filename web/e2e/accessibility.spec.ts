import { expect, test } from '@playwright/test'
import { expectNoAccessibilityViolations, raiseNcr, signedIn, skipWithoutDemoUsers } from './support.ts'

// axe on every page, as each role that may open it sees it (plan.md Phase 5). The detail page in each state of
// the lifecycle is checked by keyboard.spec.ts as it goes; this covers the pages the lifecycle does not pass.

test.beforeEach(() => skipWithoutDemoUsers())

test('the sign-in page, with and without its errors, has no accessibility violations', async ({ page }) => {
  await page.goto('/login')
  await expectNoAccessibilityViolations(page)

  await page.getByRole('button', { name: 'Sign in' }).click()

  await expect(page.getByText('Enter your email.')).toBeVisible()
  await expectNoAccessibilityViolations(page)
})

test('the not-found page has no accessibility violations', async ({ page }) => {
  await page.goto('/no-such-page')

  await expect(page.getByRole('heading', { level: 1, name: 'Page not found' })).toBeVisible()
  await expectNoAccessibilityViolations(page)
})

test("the inspector's pages have no accessibility violations", async ({ browser }) => {
  const inspector = await signedIn(browser, 'inspector')
  await expect(inspector.getByRole('status')).toHaveText('API status: healthy')
  await expectNoAccessibilityViolations(inspector)

  await inspector.goto('/ncrs/new')
  await inspector.getByRole('button', { name: 'Raise NCR' }).click()
  await expect(inspector.getByText('Enter a title.')).toBeVisible()
  await expectNoAccessibilityViolations(inspector)

  await raiseNcr(inspector, `Accessibility check ${Date.now()}`)
  await expectNoAccessibilityViolations(inspector)

  await inspector.goto('/ncrs')
  await expect(inspector.getByRole('table', { name: 'NCRs' })).toBeVisible()
  await expectNoAccessibilityViolations(inspector)

  await inspector.goto('/ncrs/00000000-0000-0000-0000-000000000000')
  await expect(inspector.getByRole('heading', { level: 1, name: 'NCR not found' })).toBeVisible()
  await expectNoAccessibilityViolations(inspector)
})

test("the investigator's pages have no accessibility violations", async ({ browser }) => {
  const investigator = await signedIn(browser, 'investigator')

  await investigator.goto('/ncrs')

  await expect(investigator.getByRole('heading', { level: 1, name: 'NCRs' })).toBeVisible()
  await expect(investigator.getByRole('status')).toHaveCount(0)
  await expectNoAccessibilityViolations(investigator)
})

test("the QA manager's pages have no accessibility violations", async ({ browser }) => {
  const manager = await signedIn(browser, 'qamanager')

  await manager.goto('/dashboard')
  await expect(manager.getByRole('heading', { level: 1, name: 'Dashboard' })).toBeVisible()
  await expect(manager.getByText('Loading', { exact: false })).toHaveCount(0)
  await expectNoAccessibilityViolations(manager)

  await manager.goto('/ncrs?overdueOnly=true')
  await expect(manager.getByRole('heading', { level: 1, name: 'NCRs' })).toBeVisible()
  await expect(manager.getByRole('status')).toHaveCount(0)
  await expectNoAccessibilityViolations(manager)
})
