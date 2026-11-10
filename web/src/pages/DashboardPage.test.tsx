import { screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { json, signedInAs, stubApi } from '../test/fakeApi.ts'
import { renderApp } from '../test/renderApp.tsx'

afterEach(() => {
  vi.unstubAllGlobals()
})

const dashboard = {
  unfinishedBySeverity: [
    { severity: 'Minor', count: 4 },
    { severity: 'Major', count: 2 },
    { severity: 'Critical', count: 1 },
  ],
  overdueCount: 3,
  averageDaysToClose: 4.25,
}

describe('DashboardPage', () => {
  it('shows unfinished NCRs by severity, the overdue count and the average days to close', async () => {
    stubApi({ ...signedInAs('QaManager'), 'GET /api/dashboard': () => json(dashboard) })

    renderApp('/dashboard')

    const bySeverity = await screen.findByRole('region', { name: 'Unfinished NCRs by severity' })
    expect(within(bySeverity).getByText('Critical').closest('div')).toHaveTextContent('1')
    expect(within(bySeverity).getByText('Major').closest('div')).toHaveTextContent('2')
    expect(within(bySeverity).getByText('Minor').closest('div')).toHaveTextContent('4')
    expect(screen.getByText('Overdue').closest('div')).toHaveTextContent('3')
    expect(screen.getByText('Average days to close').closest('div')).toHaveTextContent('4.3')
  })

  it('links the overdue count to the overdue NCRs in the list', async () => {
    stubApi({ ...signedInAs('QaManager'), 'GET /api/dashboard': () => json(dashboard) })

    renderApp('/dashboard')

    expect(await screen.findByRole('link', { name: 'See the overdue NCRs' })).toHaveAttribute(
      'href',
      '/ncrs?overdueOnly=true&sortBy=DueDate&direction=Ascending',
    )
  })

  it('says when no NCR closed in the last 90 days instead of showing an average of zero', async () => {
    stubApi({ ...signedInAs('QaManager'), 'GET /api/dashboard': () => json({ ...dashboard, averageDaysToClose: null }) })

    renderApp('/dashboard')

    expect(await screen.findByText('No NCRs closed in the last 90 days')).toBeInTheDocument()
  })

  it('says when the dashboard could not be loaded', async () => {
    stubApi({ ...signedInAs('QaManager'), 'GET /api/dashboard': () => new Response(null, { status: 500 }) })

    renderApp('/dashboard')

    expect(await screen.findByRole('alert')).toHaveTextContent('could not be loaded')
  })

  it('is in the navigation for QA managers only', async () => {
    stubApi({ ...signedInAs('Inspector'), 'GET /api/ncrs': () => json({ items: [], page: 1, hasNextPage: false }) })

    renderApp('/ncrs')

    await screen.findByRole('heading', { level: 1, name: 'NCRs' })
    expect(screen.queryByRole('link', { name: 'Dashboard' })).not.toBeInTheDocument()
  })
})
