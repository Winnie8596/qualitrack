import { screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { callsTo, json, signedInAs, stubApi } from '../test/fakeApi.ts'
import { aListPage, anNcrSummary } from '../test/ncrs.ts'
import { renderApp } from '../test/renderApp.tsx'

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('NcrListPage', () => {
  it('shows each NCR as a row with its status, severity and overdue in words, linking to its page', async () => {
    const ncr = anNcrSummary({ status: 'Investigating', severity: 'Critical', isOverdue: true, dueDate: '2026-10-01T00:00:00+00:00' })
    stubApi({ ...signedInAs('QaManager'), 'GET /api/ncrs': () => json(aListPage([ncr])) })

    renderApp('/ncrs')

    const table = await screen.findByRole('table', { name: 'NCRs' })
    const row = within(table).getAllByRole('row')[1]
    expect(row).toHaveTextContent('Investigating')
    expect(row).toHaveTextContent('Critical')
    expect(row).toHaveTextContent('Overdue')
    expect(within(row).getByRole('link', { name: 'NCR-2026-0001' })).toHaveAttribute('href', `/ncrs/${ncr.id}`)
  })

  it('asks the API for the filters chosen and puts them in the address', async () => {
    const fetchMock = stubApi({
      ...signedInAs('QaManager'),
      'GET /api/ncrs': () => json(aListPage([])),
      'GET /api/ncrs?status=PendingVerification': () => json(aListPage([])),
      'GET /api/ncrs?status=PendingVerification&overdueOnly=true': () => json(aListPage([])),
      'GET /api/ncrs?status=PendingVerification&overdueOnly=true&sortBy=DueDate&direction=Ascending': () =>
        json(aListPage([anNcrSummary()])),
    })
    const { user } = renderApp('/ncrs')

    await user.selectOptions(await screen.findByLabelText('Status'), 'Pending verification')
    await user.click(screen.getByLabelText('Overdue only'))
    await user.selectOptions(screen.getByLabelText('Sort by'), 'Due date, soonest first')

    expect(await screen.findByRole('link', { name: 'NCR-2026-0001' })).toBeInTheDocument()
    expect(
      callsTo(fetchMock, 'GET /api/ncrs?status=PendingVerification&overdueOnly=true&sortBy=DueDate&direction=Ascending'),
    ).toHaveLength(1)
  })

  it('starts from the filters in the address', async () => {
    stubApi({ ...signedInAs('QaManager'), 'GET /api/ncrs?severity=Minor': () => json(aListPage([anNcrSummary()])) })

    renderApp('/ncrs?severity=Minor')

    expect(await screen.findByRole('link', { name: 'NCR-2026-0001' })).toBeInTheDocument()
    expect(screen.getByLabelText('Severity')).toHaveValue('Minor')
  })

  it('pages forward and back, and offers no next page on the last one', async () => {
    stubApi({
      ...signedInAs('QaManager'),
      'GET /api/ncrs': () => json(aListPage([anNcrSummary()], { hasNextPage: true })),
      'GET /api/ncrs?page=2': () => json(aListPage([anNcrSummary({ number: 'NCR-2026-0021' })], { page: 2 })),
    })
    const { user } = renderApp('/ncrs')

    expect(await screen.findByRole('button', { name: 'Previous page' })).toBeDisabled()
    await user.click(screen.getByRole('button', { name: 'Next page' }))

    expect(await screen.findByRole('link', { name: 'NCR-2026-0021' })).toBeInTheDocument()
    expect(screen.getByText('Page 2')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Next page' })).toBeDisabled()
    await user.click(screen.getByRole('button', { name: 'Previous page' }))
    expect(await screen.findByRole('link', { name: 'NCR-2026-0001' })).toBeInTheDocument()
  })

  it('says so when no NCR matches', async () => {
    stubApi({ ...signedInAs('Inspector'), 'GET /api/ncrs': () => json(aListPage([])) })

    renderApp('/ncrs')

    expect(await screen.findByText('No NCRs match these filters.')).toBeInTheDocument()
  })

  it('says when the list could not be loaded', async () => {
    stubApi({ ...signedInAs('Inspector'), 'GET /api/ncrs': () => new Response(null, { status: 500 }) })

    renderApp('/ncrs')

    expect(await screen.findByRole('alert')).toHaveTextContent('could not be loaded')
  })
})
