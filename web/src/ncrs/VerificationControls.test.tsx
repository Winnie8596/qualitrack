import { screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { forgetAntiforgeryToken } from '../api/client.ts'
import type { Ncr } from '../api/ncrs.ts'
import { callsTo, json, signedInAs, stubApi, userIds } from '../test/fakeApi.ts'
import { anNcr } from '../test/ncrs.ts'
import { renderApp } from '../test/renderApp.tsx'

afterEach(() => {
  vi.unstubAllGlobals()
  forgetAntiforgeryToken()
})

const noContent = () => new Response(null, { status: 204 })

const pendingVerification = anNcr({
  status: 'PendingVerification',
  investigatorId: userIds.investigator,
  dueDate: '2026-11-05T00:00:00+00:00',
  rootCause: 'Deburring step skipped',
})

function detailRoute(initial: Ncr) {
  const state = { current: initial }
  return { state, route: { [`GET /api/ncrs/${initial.id}`]: () => json(state.current) } }
}

describe('Verification on the NCR page', () => {
  it('lets a QA manager verify the fix, which closes the NCR', async () => {
    const { state, route } = detailRoute(pendingVerification)
    const fetchMock = stubApi({
      ...signedInAs('QaManager'),
      ...route,
      [`POST /api/ncrs/${pendingVerification.id}/verify`]: () => {
        state.current = { ...state.current, status: 'Closed' }
        return noContent()
      },
    })
    const { user } = renderApp(`/ncrs/${pendingVerification.id}`)

    await user.click(await screen.findByRole('button', { name: 'Verify and close' }))

    const details = screen.getByRole('region', { name: 'Details' })
    expect(await within(details).findByText('Closed')).toBeInTheDocument()
    expect(callsTo(fetchMock, `POST /api/ncrs/${pendingVerification.id}/verify`)).toHaveLength(1)
    expect(screen.queryByRole('region', { name: 'Verification' })).not.toBeInTheDocument()
  })

  it('lets a QA manager reject the fix with a reason', async () => {
    const { state, route } = detailRoute(pendingVerification)
    const fetchMock = stubApi({
      ...signedInAs('QaManager'),
      ...route,
      [`POST /api/ncrs/${pendingVerification.id}/reject`]: () => {
        state.current = { ...state.current, status: 'ActionInProgress' }
        return noContent()
      },
    })
    const { user } = renderApp(`/ncrs/${pendingVerification.id}`)

    const reject = await screen.findByRole('form', { name: 'Reject the fix' })
    await user.click(within(reject).getByRole('button', { name: 'Reject' }))
    expect(within(reject).getByLabelText('Reason')).toHaveAccessibleDescription('Enter a reason.')
    await user.type(within(reject).getByLabelText('Reason'), 'Burr still on the inner edge')
    await user.click(within(reject).getByRole('button', { name: 'Reject' }))

    const details = screen.getByRole('region', { name: 'Details' })
    expect(await within(details).findByText('Action in progress')).toBeInTheDocument()
    expect(callsTo(fetchMock, `POST /api/ncrs/${pendingVerification.id}/reject`)[0].body).toEqual({
      reason: 'Burr still on the inner edge',
    })
  })

  it.each(['Inspector', 'Investigator'] as const)('shows no verification controls to an %s', async (role) => {
    stubApi({ ...signedInAs(role), [`GET /api/ncrs/${pendingVerification.id}`]: () => json(pendingVerification) })

    renderApp(`/ncrs/${pendingVerification.id}`)

    await screen.findByRole('region', { name: 'Details' })
    expect(screen.queryByRole('region', { name: 'Verification' })).not.toBeInTheDocument()
  })

  it('shows no verification controls before the fix is submitted', async () => {
    const ncr = { ...pendingVerification, status: 'ActionInProgress' as const }
    stubApi({ ...signedInAs('QaManager'), [`GET /api/ncrs/${ncr.id}`]: () => json(ncr) })

    renderApp(`/ncrs/${ncr.id}`)

    await screen.findByRole('region', { name: 'Details' })
    expect(screen.queryByRole('region', { name: 'Verification' })).not.toBeInTheDocument()
  })
})
