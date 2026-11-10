import { expect, test, type Browser, type Page } from '@playwright/test'
import { daysFromNow, expectStatus, raiseNcr, signedIn, skipWithoutDemoUsers, type DemoRole } from './support.ts'

// Every page at a small phone's width and a laptop's (plan.md Phase 5): nothing may make the page scroll sideways
// (a wide table scrolls inside its own box instead). A full-page screenshot of each is kept in test-results/ for
// a person to look over, since "no sideways scroll" cannot tell whether the layout also looks right.

const widths = [375, 1280] as const

async function expectFitsTheWidth(page: Page, name: string) {
  for (const width of widths) {
    await page.setViewportSize({ width, height: 800 })

    const overflow = await page.locator('html').evaluate((html) => html.scrollWidth - html.clientWidth)

    await page.screenshot({ path: test.info().outputPath(`${name}-${width}.png`), fullPage: true })
    expect(overflow, `${name} at ${width} px scrolls sideways by ${overflow} px`).toBeLessThanOrEqual(0)
  }
}

async function signedInAt(browser: Browser, role: DemoRole, path: string) {
  const page = await signedIn(browser, role)
  await page.goto(path)
  return page
}

test.beforeEach(() => skipWithoutDemoUsers())

test('the signed-out pages fit a phone and a laptop', async ({ page }) => {
  await page.goto('/login')
  await page.getByRole('button', { name: 'Sign in' }).click()
  await expect(page.getByText('Enter your email.')).toBeVisible()
  await expectFitsTheWidth(page, 'sign-in')

  await page.goto('/no-such-page')
  await expectFitsTheWidth(page, 'not-found')
})

test("every signed-in page fits a phone and a laptop, with an NCR's actions and history filled in", async ({
  browser,
}) => {
  test.setTimeout(90_000)
  const inspector = await signedIn(browser, 'inspector')
  await expect(inspector.getByRole('status')).toHaveText('API status: healthy')
  await expectFitsTheWidth(inspector, 'home')
  await inspector.goto('/ncrs/new')
  await inspector.getByRole('button', { name: 'Raise NCR' }).click()
  await expect(inspector.getByText('Enter a title.')).toBeVisible()
  await expectFitsTheWidth(inspector, 'raise')
  // A long unbroken title is the likeliest thing to push a narrow page wide
  const ncrPath = await raiseNcr(inspector, `PartNumberFLG4471209WeldSeamResponsiveCheck${Date.now()}`)
  await expectFitsTheWidth(inspector, 'detail-open')

  const manager = await signedInAt(browser, 'qamanager', ncrPath)
  await expectStatus(manager, 'Open')
  await expectFitsTheWidth(manager, 'detail-triage')
  const assign = manager.getByRole('form', { name: 'Assign an investigator' })
  await assign.getByLabel('Investigator').selectOption({ label: 'investigator@qualitrack.demo' })
  await assign.getByLabel('Due').fill(daysFromNow(5).value)
  await assign.getByRole('button', { name: 'Assign' }).click()
  await expectStatus(manager, 'Investigating')

  const investigator = await signedInAt(browser, 'investigator', ncrPath)
  await expectFitsTheWidth(investigator, 'detail-plan-fix')
  const plan = investigator.getByRole('form', { name: 'Plan the fix' })
  await plan.getByLabel('Root cause').fill('Deburring step skipped on night shift')
  await plan.getByLabel('Corrective action 1').fill('Deburr the flange')
  await plan.getByRole('button', { name: 'Plan the fix' }).click()
  await expectStatus(investigator, 'Action in progress')
  await expectFitsTheWidth(investigator, 'detail-actions')

  await manager.goto('/ncrs')
  await expect(manager.getByRole('table', { name: 'NCRs' })).toBeVisible()
  await expectFitsTheWidth(manager, 'list')
  await manager.goto('/dashboard')
  await expect(manager.getByText('Loading', { exact: false })).toHaveCount(0)
  await expectFitsTheWidth(manager, 'dashboard')
})
