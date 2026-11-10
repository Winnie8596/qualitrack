import { screen } from '@testing-library/react'
import type { UserEvent } from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { forgetAntiforgeryToken } from '../api/client.ts'
import { callsTo, json, problem, signedInAs, stubApi } from '../test/fakeApi.ts'
import { renderApp } from '../test/renderApp.tsx'
import { anNcr } from '../test/ncrs.ts'

afterEach(() => {
  vi.unstubAllGlobals()
  forgetAntiforgeryToken()
})

async function fillIn(user: UserEvent) {
  await user.type(await screen.findByLabelText('Title'), '  Weld crack on bracket  ')
  await user.type(screen.getByLabelText('Description'), 'Hairline crack along the fillet weld.')
  await user.type(screen.getByLabelText('Location'), 'Bay 3')
  await user.click(screen.getByRole('radio', { name: 'Critical' }))
}

describe('RaiseNcrPage', () => {
  it('asks for every field before sending anything, and treats spaces as blank', async () => {
    const fetchMock = stubApi(signedInAs('Inspector'))
    const { user } = renderApp('/ncrs/new')

    await user.type(await screen.findByLabelText('Title'), '   ')
    await user.click(screen.getByRole('button', { name: 'Raise NCR' }))

    expect(screen.getByLabelText('Title')).toHaveAccessibleDescription('Enter a title.')
    expect(screen.getByLabelText('Description')).toHaveAccessibleDescription('Enter a description.')
    expect(screen.getByLabelText('Location')).toHaveAccessibleDescription('Enter a location.')
    expect(screen.getByRole('radiogroup', { name: 'Severity' })).toHaveAccessibleDescription('Choose a severity.')
    expect(callsTo(fetchMock, 'POST /api/ncrs')).toHaveLength(0)
  })

  it('raises the NCR with the antiforgery token and opens its detail page', async () => {
    const ncr = anNcr({ title: 'Weld crack on bracket', severity: 'Critical' })
    const fetchMock = stubApi({
      ...signedInAs('Inspector'),
      'POST /api/ncrs': () => json({ id: ncr.id }, 201),
      [`GET /api/ncrs/${ncr.id}`]: () => json(ncr),
    })
    const { user } = renderApp('/ncrs/new')

    await fillIn(user)
    await user.click(screen.getByRole('button', { name: 'Raise NCR' }))

    expect(await screen.findByRole('heading', { level: 1, name: /Weld crack on bracket/ })).toBeInTheDocument()
    const [raise] = callsTo(fetchMock, 'POST /api/ncrs')
    expect(raise.body).toEqual({
      title: 'Weld crack on bracket',
      description: 'Hairline crack along the fillet weld.',
      location: 'Bay 3',
      severity: 'Critical',
    })
    expect(raise.headers.get('X-XSRF-TOKEN')).toBe('test-token')
  })

  it('puts the errors the API returns on the fields they belong to', async () => {
    stubApi({
      ...signedInAs('Inspector'),
      'POST /api/ncrs': () =>
        problem(400, {
          title: 'One or more validation errors occurred.',
          errors: { location: ['The Location field is required.'] },
        }),
    })
    const { user } = renderApp('/ncrs/new')

    await fillIn(user)
    await user.click(screen.getByRole('button', { name: 'Raise NCR' }))

    expect(await screen.findByText('The Location field is required.')).toBeInTheDocument()
    expect(screen.getByLabelText('Location')).toHaveAccessibleDescription('The Location field is required.')
  })

  it('shows a refusal that is not about one field above the form', async () => {
    stubApi({
      ...signedInAs('Inspector'),
      'POST /api/ncrs': () => problem(403, { title: 'Forbidden', detail: 'Only inspectors can raise NCRs.' }),
    })
    const { user } = renderApp('/ncrs/new')

    await fillIn(user)
    await user.click(screen.getByRole('button', { name: 'Raise NCR' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Only inspectors can raise NCRs.')
  })

  it('attaches the photo chosen once the NCR is raised, then opens it', async () => {
    const ncr = anNcr({ hasPhoto: true })
    const fetchMock = stubApi({
      ...signedInAs('Inspector'),
      'POST /api/ncrs': () => json({ id: ncr.id }, 201),
      [`POST /api/ncrs/${ncr.id}/photo`]: () => new Response(null, { status: 204 }),
      [`GET /api/ncrs/${ncr.id}`]: () => json(ncr),
    })
    const { user } = renderApp('/ncrs/new')
    const photo = new File([new Uint8Array([0x89, 0x50, 0x4e, 0x47])], 'weld.png', { type: 'image/png' })

    await fillIn(user)
    await user.upload(screen.getByLabelText('Photo (optional)'), photo)
    await user.click(screen.getByRole('button', { name: 'Raise NCR' }))

    expect(await screen.findByRole('img', { name: /Photo of/ })).toHaveAttribute('src', `/api/ncrs/${ncr.id}/photo`)
    const upload = fetchMock.mock.calls.find(([url, init]) => url === `/api/ncrs/${ncr.id}/photo` && init?.method === 'POST')
    const body = upload?.[1]?.body
    expect(body).toBeInstanceOf(FormData)
    expect((body as FormData).get('photo')).toBe(photo)
    expect(new Headers(upload?.[1]?.headers).get('X-XSRF-TOKEN')).toBe('test-token')
  })

  it.each([
    ['a GIF', new File(['GIF89a'], 'weld.gif', { type: 'image/gif' }), 'Choose a JPEG or PNG image.'],
    ['a file over 5 MB', new File([new Uint8Array(5 * 1024 * 1024 + 1)], 'weld.png', { type: 'image/png' }), 'Choose a photo of 5 MB or less.'],
  ])('refuses %s before sending anything', async (_what, photo, message) => {
    const fetchMock = stubApi(signedInAs('Inspector'))
    const { user } = renderApp('/ncrs/new')

    await fillIn(user)
    // applyAccept off: the browser's picker filters by type, but a user can still drop in any file
    await userWithoutAccept(user).upload(screen.getByLabelText('Photo (optional)'), photo)
    await user.click(screen.getByRole('button', { name: 'Raise NCR' }))

    expect(screen.getByLabelText('Photo (optional)')).toHaveAccessibleDescription(message)
    expect(callsTo(fetchMock, 'POST /api/ncrs')).toHaveLength(0)
  })

  it('still opens the raised NCR when its photo could not be attached, and says so', async () => {
    const ncr = anNcr()
    stubApi({
      ...signedInAs('Inspector'),
      'POST /api/ncrs': () => json({ id: ncr.id }, 201),
      [`POST /api/ncrs/${ncr.id}/photo`]: () => problem(400, { detail: 'A photo must be a JPEG or PNG image.' }),
      [`GET /api/ncrs/${ncr.id}`]: () => json(ncr),
    })
    const { user } = renderApp('/ncrs/new')

    await fillIn(user)
    await user.upload(screen.getByLabelText('Photo (optional)'), new File([new Uint8Array([1])], 'weld.png', { type: 'image/png' }))
    await user.click(screen.getByRole('button', { name: 'Raise NCR' }))

    expect(await screen.findByRole('heading', { level: 1, name: /Weld crack on bracket/ })).toBeInTheDocument()
    expect(screen.getByRole('alert')).toHaveTextContent('raised, but its photo could not be attached')
  })
})

function userWithoutAccept(user: UserEvent): UserEvent {
  return user.setup({ applyAccept: false })
}
