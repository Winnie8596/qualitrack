import { expect, test, type Browser, type Page } from '@playwright/test'

// Each role signs in in its own browser context, as three people on three machines would, and does its part
// of one NCR's life: raise, assign, plan and complete the fix, reject once, fix again, verify.

const demoPassword = process.env.E2E_DEMO_PASSWORD ?? ''

type DemoRole = 'inspector' | 'investigator' | 'qamanager'

async function signedIn(browser: Browser, role: DemoRole): Promise<Page> {
  const page = await (await browser.newContext()).newPage()
  await page.goto('/login')
  await page.getByLabel('Email').fill(`${role}@qualitrack.demo`)
  await page.getByLabel('Password').fill(demoPassword)
  await page.getByRole('button', { name: 'Sign in' }).click()
  await expect(page.getByRole('button', { name: 'Sign out' })).toBeVisible()
  return page
}

// A datetime-local value, in the browser's own time zone
function daysFromNow(days: number): string {
  const due = new Date(Date.now() + days * 24 * 60 * 60 * 1000)
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${due.getFullYear()}-${pad(due.getMonth() + 1)}-${pad(due.getDate())}T${pad(due.getHours())}:${pad(due.getMinutes())}`
}

async function expectStatus(page: Page, status: string) {
  const details = page.getByRole('region', { name: 'Details' })
  await expect(details.getByText(status, { exact: true })).toBeVisible()
}

test('an NCR goes from raised to closed with each role doing its part in the browser', async ({ browser }) => {
  test.skip(!demoPassword, 'Set E2E_DEMO_PASSWORD (and E2E_CONNECTION_STRING) to run the browser smoke test')
  const title = `Burr on flange ${Date.now()}`

  // A1: the inspector raises it and lands on its page
  const inspector = await signedIn(browser, 'inspector')
  await inspector.getByRole('link', { name: 'Raise an NCR' }).click()
  await inspector.getByLabel('Title').fill(title)
  await inspector.getByLabel('Description').fill('Sharp burr on the mating face.')
  await inspector.getByLabel('Location').fill('Line 3')
  await inspector.getByRole('radio', { name: 'Major' }).check()
  await inspector.getByRole('button', { name: 'Raise NCR' }).click()
  await expect(inspector.getByRole('heading', { level: 1, name: new RegExp(title) })).toBeVisible()
  await expectStatus(inspector, 'Open')
  const ncrPath = new URL(inspector.url()).pathname

  // D1 and A2: the QA manager finds it in the list and assigns the investigator
  const manager = await signedIn(browser, 'qamanager')
  await manager.getByRole('link', { name: 'NCRs', exact: true }).click()
  await manager.getByLabel('Status').selectOption('Open')
  await manager.getByRole('row', { name: new RegExp(title) }).getByRole('link').click()
  const assign = manager.getByRole('form', { name: 'Assign an investigator' })
  await assign.getByLabel('Investigator').selectOption({ label: 'investigator@qualitrack.demo' })
  await assign.getByLabel('Due').fill(daysFromNow(5))
  await assign.getByRole('button', { name: 'Assign' }).click()
  await expectStatus(manager, 'Investigating')

  // B1–B3: the investigator plans the fix, completes the action and submits
  const investigator = await signedIn(browser, 'investigator')
  await investigator.goto(ncrPath)
  const plan = investigator.getByRole('form', { name: 'Plan the fix' })
  await plan.getByLabel('Root cause').fill('Deburring step skipped on night shift')
  await plan.getByLabel('Corrective action 1').fill('Deburr the flange')
  await plan.getByRole('button', { name: 'Plan the fix' }).click()
  await expectStatus(investigator, 'Action in progress')
  await investigator.getByRole('button', { name: 'Mark "Deburr the flange" complete' }).click()
  await investigator.getByRole('button', { name: 'Submit for verification' }).click()
  await expectStatus(investigator, 'Pending verification')

  // C2: the QA manager rejects it once
  await manager.reload()
  const reject = manager.getByRole('form', { name: 'Reject the fix' })
  await reject.getByLabel('Reason').fill('Burr still on the inner edge')
  await reject.getByRole('button', { name: 'Reject' }).click()
  await expectStatus(manager, 'Action in progress')

  // B2–B3 again: one more action, completed, resubmitted
  await investigator.reload()
  const add = investigator.getByRole('form', { name: 'Add a corrective action' })
  await add.getByLabel('Description').fill('Deburr the inner edge')
  await add.getByRole('button', { name: 'Add action' }).click()
  await investigator.getByRole('button', { name: 'Mark "Deburr the inner edge" complete' }).click()
  await investigator.getByRole('button', { name: 'Submit for verification' }).click()
  await expectStatus(investigator, 'Pending verification')

  // C1: the QA manager verifies, and the inspector sees it closed with its whole history
  await manager.reload()
  await manager.getByRole('button', { name: 'Verify and close' }).click()
  await expectStatus(manager, 'Closed')

  await inspector.reload()
  await expectStatus(inspector, 'Closed')
  await expect(inspector.getByRole('region', { name: 'History' }).getByRole('listitem')).toHaveCount(6)
})
