import { screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { callsTo, json, problem, signedInAs, stubApi, userIds } from '../test/fakeApi.ts'
import { anNcr } from '../test/ncrs.ts'
import { renderApp } from '../test/renderApp.tsx'

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('NcrDetailPage', () => {
  it('shows the NCR with its status and severity in words', async () => {
    const ncr = anNcr({ status: 'Open', severity: 'Critical', location: 'Bay 3' })
    stubApi({ ...signedInAs('Inspector'), [`GET /api/ncrs/${ncr.id}`]: () => json(ncr) })

    renderApp(`/ncrs/${ncr.id}`)

    expect(
      await screen.findByRole('heading', { level: 1, name: 'NCR-2026-0001: Weld crack on bracket' }),
    ).toBeInTheDocument()
    const details = screen.getByRole('region', { name: 'Details' })
    expect(within(details).getByText('Status').nextElementSibling).toHaveTextContent('Open')
    expect(within(details).getByText('Severity').nextElementSibling).toHaveTextContent('Critical')
    expect(within(details).getByText('Location').nextElementSibling).toHaveTextContent('Bay 3')
    expect(within(details).getByText('Raised by').nextElementSibling).toHaveTextContent('You')
    expect(within(details).getByText('Investigator').nextElementSibling).toHaveTextContent('Not assigned yet')
  })

  it('lists the status history with notes', async () => {
    const ncr = anNcr({
      status: 'Investigating',
      investigatorId: userIds.investigator,
      dueDate: '2026-11-05T00:00:00+00:00',
      history: [
        { from: 'Open', to: 'Investigating', by: userIds.qaManager, at: '2026-10-30T09:00:00+00:00', note: 'Urgent' },
      ],
    })
    stubApi({ ...signedInAs('Inspector'), [`GET /api/ncrs/${ncr.id}`]: () => json(ncr) })

    renderApp(`/ncrs/${ncr.id}`)

    const history = await screen.findByRole('region', { name: 'History' })
    expect(within(history).getByRole('listitem')).toHaveTextContent('Open → Investigating')
    expect(within(history).getByRole('listitem')).toHaveTextContent('Urgent')
  })

  it('says an NCR is overdue in words', async () => {
    const ncr = anNcr({ status: 'Investigating', isOverdue: true, dueDate: '2026-10-01T00:00:00+00:00' })
    stubApi({ ...signedInAs('Inspector'), [`GET /api/ncrs/${ncr.id}`]: () => json(ncr) })

    renderApp(`/ncrs/${ncr.id}`)

    expect(await screen.findByText('Overdue')).toBeInTheDocument()
  })

  it('shows not found when the API has no NCR the user may see', async () => {
    const id = '0199a1b2-c3d4-7e5f-8a9b-000000000000'
    stubApi({ ...signedInAs('Inspector'), [`GET /api/ncrs/${id}`]: () => problem(404, { title: 'Not Found' }) })

    renderApp(`/ncrs/${id}`)

    expect(await screen.findByRole('heading', { level: 1, name: 'NCR not found' })).toBeInTheDocument()
  })

  it('shows not found for an address that cannot be an NCR, without asking the API', async () => {
    const fetchMock = stubApi(signedInAs('Inspector'))

    renderApp('/ncrs/not-an-id')

    expect(await screen.findByRole('heading', { level: 1, name: 'NCR not found' })).toBeInTheDocument()
    expect(fetchMock.mock.calls.some(([url]) => String(url).startsWith('/api/ncrs/'))).toBe(false)
    expect(callsTo(fetchMock, 'GET /api/ncrs/not-an-id')).toHaveLength(0)
  })

  it('says when the NCR could not be loaded for another reason', async () => {
    const ncr = anNcr()
    stubApi({ ...signedInAs('Inspector'), [`GET /api/ncrs/${ncr.id}`]: () => new Response(null, { status: 500 }) })

    renderApp(`/ncrs/${ncr.id}`)

    expect(await screen.findByRole('alert')).toHaveTextContent('could not be loaded')
  })
})
