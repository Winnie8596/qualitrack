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

const noContent = () => new Response(null, { status: 204 })

const investigating = anNcr({
  status: 'Investigating',
  investigatorId: userIds.investigator,
  dueDate: '2026-11-05T00:00:00+00:00',
})

const actionInProgress: Ncr = {
  ...investigating,
  status: 'ActionInProgress',
  rootCause: 'Deburring step skipped',
  actions: [
    {
      id: '0199a1b2-0000-7000-8000-00000000000a',
      description: 'Deburr the flange',
      addedBy: userIds.investigator,
      addedAt: '2026-10-31T09:00:00+00:00',
      completedBy: null,
      completedAt: null,
    },
  ],
}

/** The detail route answers with whatever `state.current` holds, so a command can change the NCR. */
function detailRoute(initial: Ncr) {
  const state = { current: initial }
  return { state, route: { [`GET /api/ncrs/${initial.id}`]: () => json(state.current) } }
}

describe('Investigation on the NCR page', () => {
  it('lets the assigned investigator plan the fix with a root cause and several actions', async () => {
    const { state, route } = detailRoute(investigating)
    const fetchMock = stubApi({
      ...signedInAs('Investigator'),
      ...route,
      [`POST /api/ncrs/${investigating.id}/plan-fix`]: () => {
        state.current = actionInProgress
        return noContent()
      },
    })
    const { user } = renderApp(`/ncrs/${investigating.id}`)

    const form = await screen.findByRole('form', { name: 'Plan the fix' })
    await user.type(within(form).getByLabelText('Root cause'), 'Deburring step skipped')
    await user.type(within(form).getByLabelText('Corrective action 1'), 'Deburr the flange')
    await user.click(within(form).getByRole('button', { name: 'Add another action' }))
    await user.type(within(form).getByLabelText('Corrective action 2'), 'Update the checklist')
    await user.click(within(form).getByRole('button', { name: 'Plan the fix' }))

    expect(await screen.findByRole('form', { name: 'Add a corrective action' })).toBeInTheDocument()
    expect(callsTo(fetchMock, `POST /api/ncrs/${investigating.id}/plan-fix`)[0].body).toEqual({
      rootCause: 'Deburring step skipped',
      actions: ['Deburr the flange', 'Update the checklist'],
    })
  })

  it('asks for a root cause and a description for every action before sending anything', async () => {
    const fetchMock = stubApi({ ...signedInAs('Investigator'), [`GET /api/ncrs/${investigating.id}`]: () => json(investigating) })
    const { user } = renderApp(`/ncrs/${investigating.id}`)

    const form = await screen.findByRole('form', { name: 'Plan the fix' })
    await user.click(within(form).getByRole('button', { name: 'Plan the fix' }))

    expect(within(form).getByLabelText('Root cause')).toHaveAccessibleDescription('Enter the root cause.')
    expect(within(form).getByLabelText('Corrective action 1')).toHaveAccessibleDescription('Describe the action.')
    expect(callsTo(fetchMock, `POST /api/ncrs/${investigating.id}/plan-fix`)).toHaveLength(0)
  })

  it('lets the investigator complete an action and add another', async () => {
    const { state, route } = detailRoute(actionInProgress)
    const [action] = actionInProgress.actions
    const fetchMock = stubApi({
      ...signedInAs('Investigator'),
      ...route,
      [`POST /api/ncrs/${actionInProgress.id}/actions/${action.id}/complete`]: () => {
        state.current = {
          ...state.current,
          actions: [{ ...action, completedBy: userIds.investigator, completedAt: '2026-11-01T09:00:00+00:00' }],
        }
        return noContent()
      },
      [`POST /api/ncrs/${actionInProgress.id}/actions`]: () => noContent(),
    })
    const { user } = renderApp(`/ncrs/${actionInProgress.id}`)

    await user.click(await screen.findByRole('button', { name: 'Mark "Deburr the flange" complete' }))
    const actions = screen.getByRole('region', { name: 'Corrective actions' })
    expect(await within(actions).findByText(/completed/)).toBeInTheDocument()
    const add = screen.getByRole('form', { name: 'Add a corrective action' })
    await user.type(within(add).getByLabelText('Description'), 'Re-train the night shift')
    await user.click(within(add).getByRole('button', { name: 'Add action' }))

    expect(callsTo(fetchMock, `POST /api/ncrs/${actionInProgress.id}/actions`)[0].body).toEqual({
      description: 'Re-train the night shift',
    })
  })

  it('submits for verification, and says why when the API refuses', async () => {
    const fetchMock = stubApi({
      ...signedInAs('Investigator'),
      [`GET /api/ncrs/${actionInProgress.id}`]: () => json(actionInProgress),
      [`POST /api/ncrs/${actionInProgress.id}/submit`]: () =>
        problem(409, { detail: 'Every corrective action must be completed before submitting for verification.' }),
    })
    const { user } = renderApp(`/ncrs/${actionInProgress.id}`)

    await user.click(await screen.findByRole('button', { name: 'Submit for verification' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Every corrective action must be completed')
    expect(callsTo(fetchMock, `POST /api/ncrs/${actionInProgress.id}/submit`)).toHaveLength(1)
  })

  it.each([
    ['another investigator', 'Investigator', '6f1c2b8e-0000-4000-8000-0000000000ff'],
    ['a QA manager', 'QaManager', userIds.investigator],
  ] as const)('shows no investigation controls to %s', async (_who, role, investigatorId) => {
    const ncr = { ...actionInProgress, investigatorId }
    stubApi({ ...signedInAs(role), [`GET /api/ncrs/${ncr.id}`]: () => json(ncr) })

    renderApp(`/ncrs/${ncr.id}`)

    await screen.findByRole('region', { name: 'Corrective actions' })
    expect(screen.queryByRole('form', { name: 'Add a corrective action' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /complete/ })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Submit for verification' })).not.toBeInTheDocument()
  })
})
