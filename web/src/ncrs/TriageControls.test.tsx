import { screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { forgetAntiforgeryToken } from '../api/client.ts'
import type { Ncr } from '../api/ncrs.ts'
import { callsTo, json, problem, signedInAs, stubApi, userIds } from '../test/fakeApi.ts'
import { anNcr } from '../test/ncrs.ts'
import { renderApp } from '../test/renderApp.tsx'

afterEach(() => {
  vi.unstubAllGlobals()
  forgetAntiforgeryToken()
})

const investigators = [{ id: userIds.investigator, email: 'bea@example.com' }]

const noContent = () => new Response(null, { status: 204 })

/** The detail route answers with whatever `current` holds, so a test can change the NCR after a command. */
function detailRoute(initial: Ncr) {
  const state = { current: initial }
  return { state, route: { [`GET /api/ncrs/${initial.id}`]: () => json(state.current) } }
}

describe('Triage on the NCR page', () => {
  it('lets a QA manager assign an investigator with a due date, then shows the NCR being investigated', async () => {
    const { state, route } = detailRoute(anNcr({ status: 'Open' }))
    const fetchMock = stubApi({
      ...signedInAs('QaManager'),
      ...route,
      'GET /api/users/investigators': () => json(investigators),
      [`POST /api/ncrs/${state.current.id}/assign`]: () => {
        state.current = { ...state.current, status: 'Investigating', investigatorId: userIds.investigator }
        return noContent()
      },
    })
    const { user } = renderApp(`/ncrs/${state.current.id}`)

    const assign = await screen.findByRole('form', { name: 'Assign an investigator' })
    await user.selectOptions(await within(assign).findByLabelText('Investigator'), 'bea@example.com')
    await user.type(within(assign).getByLabelText('Due'), '2026-11-04T17:00')
    await user.click(within(assign).getByRole('button', { name: 'Assign' }))

    const details = await screen.findByRole('region', { name: 'Details' })
    expect(await within(details).findByText('Investigating')).toBeInTheDocument()
    const [request] = callsTo(fetchMock, `POST /api/ncrs/${state.current.id}/assign`)
    expect(request.body).toEqual({
      investigatorId: userIds.investigator,
      dueDate: new Date('2026-11-04T17:00').toISOString(),
    })
    expect(request.headers.get('X-XSRF-TOKEN')).toBe('test-token')
    expect(screen.queryByRole('form', { name: 'Assign an investigator' })).not.toBeInTheDocument()
  })

  it('asks for an investigator and a due date before sending anything', async () => {
    const ncr = anNcr({ status: 'Open' })
    const fetchMock = stubApi({
      ...signedInAs('QaManager'),
      [`GET /api/ncrs/${ncr.id}`]: () => json(ncr),
      'GET /api/users/investigators': () => json(investigators),
    })
    const { user } = renderApp(`/ncrs/${ncr.id}`)

    const assign = await screen.findByRole('form', { name: 'Assign an investigator' })
    await user.click(within(assign).getByRole('button', { name: 'Assign' }))

    expect(within(assign).getByLabelText('Investigator')).toHaveAccessibleDescription('Choose an investigator.')
    expect(within(assign).getByLabelText('Due')).toHaveAccessibleDescription('Choose a due date.')
    expect(callsTo(fetchMock, `POST /api/ncrs/${ncr.id}/assign`)).toHaveLength(0)
  })

  it('shows the rule the API refused the assignment for', async () => {
    const ncr = anNcr({ status: 'Open', severity: 'Critical' })
    stubApi({
      ...signedInAs('QaManager'),
      [`GET /api/ncrs/${ncr.id}`]: () => json(ncr),
      'GET /api/users/investigators': () => json(investigators),
      [`POST /api/ncrs/${ncr.id}/assign`]: () =>
        problem(400, { detail: 'A critical NCR must be due within 7 days of being raised.' }),
    })
    const { user } = renderApp(`/ncrs/${ncr.id}`)

    const assign = await screen.findByRole('form', { name: 'Assign an investigator' })
    await user.selectOptions(await within(assign).findByLabelText('Investigator'), 'bea@example.com')
    await user.type(within(assign).getByLabelText('Due'), '2026-12-01T17:00')
    await user.click(within(assign).getByRole('button', { name: 'Assign' }))

    expect(await within(assign).findByRole('alert')).toHaveTextContent('must be due within 7 days')
  })

  it('lets a QA manager cancel an NCR with a reason', async () => {
    const { state, route } = detailRoute(anNcr({ status: 'Open' }))
    const fetchMock = stubApi({
      ...signedInAs('QaManager'),
      ...route,
      'GET /api/users/investigators': () => json(investigators),
      [`POST /api/ncrs/${state.current.id}/cancel`]: () => {
        state.current = { ...state.current, status: 'Cancelled' }
        return noContent()
      },
    })
    const { user } = renderApp(`/ncrs/${state.current.id}`)

    const cancel = await screen.findByRole('form', { name: 'Cancel this NCR' })
    await user.click(within(cancel).getByRole('button', { name: 'Cancel NCR' }))
    expect(within(cancel).getByLabelText('Reason')).toHaveAccessibleDescription('Enter a reason.')
    await user.type(within(cancel).getByLabelText('Reason'), 'Raised by mistake')
    await user.click(within(cancel).getByRole('button', { name: 'Cancel NCR' }))

    const details = await screen.findByRole('region', { name: 'Details' })
    expect(await within(details).findByText('Cancelled')).toBeInTheDocument()
    expect(callsTo(fetchMock, `POST /api/ncrs/${state.current.id}/cancel`)[0].body).toEqual({ reason: 'Raised by mistake' })
    expect(screen.queryByRole('form', { name: 'Cancel this NCR' })).not.toBeInTheDocument()
  })

  it('offers only cancelling once the NCR is past triage', async () => {
    const ncr = anNcr({ status: 'Investigating', investigatorId: userIds.investigator })
    stubApi({ ...signedInAs('QaManager'), [`GET /api/ncrs/${ncr.id}`]: () => json(ncr) })

    renderApp(`/ncrs/${ncr.id}`)

    expect(await screen.findByRole('form', { name: 'Cancel this NCR' })).toBeInTheDocument()
    expect(screen.queryByRole('form', { name: 'Assign an investigator' })).not.toBeInTheDocument()
  })

  it.each(['Inspector', 'Investigator'] as const)('shows no triage controls to an %s', async (role) => {
    const ncr = anNcr({ status: 'Open' })
    stubApi({ ...signedInAs(role), [`GET /api/ncrs/${ncr.id}`]: () => json(ncr) })

    renderApp(`/ncrs/${ncr.id}`)

    await screen.findByRole('region', { name: 'Details' })
    expect(screen.queryByRole('form')).not.toBeInTheDocument()
  })
})
