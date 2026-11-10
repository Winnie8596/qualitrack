import { expect, test, type Browser, type Locator, type Page } from '@playwright/test'
import {
  demoEmail,
  demoPassword,
  expectNoAccessibilityViolations,
  expectStatus,
  skipWithoutDemoUsers,
  tabAndPress,
  tabAndType,
  tabTo,
} from './support.ts'

// The whole lifecycle again (plan.md Phase 5), this time with the keyboard alone: no clicks, no fill().
// Every control is reached with Tab and worked with Enter, Space, arrow keys or typing, and each page the
// lifecycle passes through is checked with axe. The photo is left out: picking a file happens in the
// operating system's dialog, outside the page.

async function signInWithKeyboard(browser: Browser, role: 'inspector' | 'investigator' | 'qamanager') {
  const page = await (await browser.newContext()).newPage()
  await page.goto('/login')
  await expectNoAccessibilityViolations(page)
  await tabAndType(page, page.getByLabel('Email'), demoEmail(role))
  await tabAndType(page, page.getByLabel('Password'), demoPassword)
  await page.keyboard.press('Enter')
  await expect(page.getByRole('button', { name: 'Sign out' })).toBeVisible()
  return page
}

/** Native select: typing an option's first letters picks it, as it does for a keyboard user. */
async function tabAndChoose(page: Page, select: Locator, option: string) {
  // Some options load from the API; a user would wait for them too
  await expect(select.getByRole('option', { name: option, exact: true })).toBeAttached()
  await tabTo(page, select)
  await page.keyboard.type(option)
  await expect(select.locator('option:checked')).toHaveText(option)
}

/** Radio group: Tab enters the group, the arrow keys move the choice. */
async function tabAndPick(page: Page, firstRadio: Locator, choice: Locator) {
  await tabTo(page, firstRadio)
  for (let presses = 0; presses < 5 && !(await choice.isChecked()); presses++) {
    await page.keyboard.press('ArrowRight')
  }
  await expect(choice).toBeChecked()
}

/**
 * The next future date whose day and month are the same number (11 November, then 12 December, 1 January…).
 * Chromium lays out a datetime-local's segments in the operating system's date order (day first on this
 * machine, month first on the CI runner) whatever the page's locale, so typing day and month alike works for both.
 */
function nextDayEqualToMonth() {
  const date = new Date()
  do {
    date.setDate(date.getDate() + 1)
  } while (date.getDate() !== date.getMonth() + 1)
  const twoDigits = String(date.getDate()).padStart(2, '0')
  return { dayAndMonth: twoDigits, year: String(date.getFullYear()) }
}

/** datetime-local: each segment takes its digits and moves on by itself, except the year, which Tab leaves. */
async function tabAndTypeDueDate(page: Page, field: Locator) {
  const { dayAndMonth, year } = nextDayEqualToMonth()
  await tabTo(page, field)
  await page.keyboard.type(`${dayAndMonth}${dayAndMonth}${year}`)
  await page.keyboard.press('Tab')
  // 10:00, then AM in a 12-hour layout (a 24-hour one has no AM/PM segment and ignores the A)
  await page.keyboard.type('1000A')
  await expect(field).toHaveValue(`${year}-${dayAndMonth}-${dayAndMonth}T10:00`)
}

async function expectStatusAndNoViolations(page: Page, status: string) {
  await expectStatus(page, status)
  await expectNoAccessibilityViolations(page)
}

test('the whole lifecycle can be run with the keyboard alone', async ({ browser }) => {
  skipWithoutDemoUsers()
  // Three roles, a dozen pages, every one checked with axe: far longer than a single-step test
  test.setTimeout(120_000)
  const title = `Keyboard burr ${Date.now()}`

  // A1: the inspector raises it
  const inspector = await signInWithKeyboard(browser, 'inspector')
  await tabAndPress(inspector, inspector.getByRole('link', { name: 'Raise an NCR' }))
  await expect(inspector.getByRole('heading', { level: 1, name: 'Raise an NCR' })).toBeVisible()
  await tabAndType(inspector, inspector.getByLabel('Title'), title)
  await tabAndType(inspector, inspector.getByLabel('Description'), 'Sharp burr on the mating face.')
  await tabAndType(inspector, inspector.getByLabel('Location'), 'Line 3')
  await tabAndPick(
    inspector,
    inspector.getByRole('radio', { name: 'Minor' }),
    inspector.getByRole('radio', { name: 'Major' }),
  )
  await expectNoAccessibilityViolations(inspector)
  await tabAndPress(inspector, inspector.getByRole('button', { name: 'Raise NCR' }))
  await expect(inspector.getByRole('heading', { level: 1, name: new RegExp(title) })).toBeVisible()
  await expectStatusAndNoViolations(inspector, 'Open')
  const ncrPath = new URL(inspector.url()).pathname

  // D1 and A2: the QA manager filters the list, opens it and assigns the investigator
  const manager = await signInWithKeyboard(browser, 'qamanager')
  await tabAndPress(manager, manager.getByRole('link', { name: 'NCRs', exact: true }))
  await tabAndChoose(manager, manager.getByLabel('Status'), 'Open')
  await expect(manager.getByRole('row', { name: new RegExp(title) })).toBeVisible()
  await expectNoAccessibilityViolations(manager)
  await tabAndPress(manager, manager.getByRole('row', { name: new RegExp(title) }).getByRole('link'))
  await expectStatusAndNoViolations(manager, 'Open')
  const assign = manager.getByRole('form', { name: 'Assign an investigator' })
  await tabAndChoose(manager, assign.getByLabel('Investigator'), 'investigator@qualitrack.demo')
  await tabAndTypeDueDate(manager, assign.getByLabel('Due'))
  await tabAndPress(manager, assign.getByRole('button', { name: 'Assign' }))
  await expectStatusAndNoViolations(manager, 'Investigating')

  // B1–B3: the investigator plans the fix, completes the action and submits
  const investigator = await signInWithKeyboard(browser, 'investigator')
  await investigator.goto(ncrPath)
  await expectStatusAndNoViolations(investigator, 'Investigating')
  const plan = investigator.getByRole('form', { name: 'Plan the fix' })
  await tabAndType(investigator, plan.getByLabel('Root cause'), 'Deburring step skipped on night shift')
  await tabAndType(investigator, plan.getByLabel('Corrective action 1'), 'Deburr the flange')
  await tabAndPress(investigator, plan.getByRole('button', { name: 'Plan the fix' }))
  await expectStatusAndNoViolations(investigator, 'Action in progress')
  await tabAndPress(investigator, investigator.getByRole('button', { name: 'Mark complete: Deburr the flange' }))
  await tabAndPress(investigator, investigator.getByRole('button', { name: 'Submit for verification' }))
  await expectStatusAndNoViolations(investigator, 'Pending verification')

  // C2: the QA manager rejects it once
  await manager.reload()
  await expectNoAccessibilityViolations(manager)
  const reject = manager.getByRole('form', { name: 'Reject the fix' })
  await tabAndType(manager, reject.getByLabel('Reason'), 'Burr still on the inner edge')
  await tabAndPress(manager, reject.getByRole('button', { name: 'Reject' }))
  await expectStatus(manager, 'Action in progress')

  // B2–B3 again: one more action, completed, resubmitted
  await investigator.reload()
  const add = investigator.getByRole('form', { name: 'Add a corrective action' })
  await tabAndType(investigator, add.getByLabel('Description'), 'Deburr the inner edge')
  await tabAndPress(investigator, add.getByRole('button', { name: 'Add action' }))
  await tabAndPress(investigator, investigator.getByRole('button', { name: 'Mark complete: Deburr the inner edge' }))
  await tabAndPress(investigator, investigator.getByRole('button', { name: 'Submit for verification' }))
  await expectStatus(investigator, 'Pending verification')

  // C1: the QA manager verifies, and the inspector sees it closed
  await manager.reload()
  await tabAndPress(manager, manager.getByRole('button', { name: 'Verify and close' }))
  await expectStatusAndNoViolations(manager, 'Closed')

  await inspector.reload()
  await expectStatusAndNoViolations(inspector, 'Closed')
  await expect(inspector.getByRole('region', { name: 'History' }).getByRole('listitem')).toHaveCount(6)
})
