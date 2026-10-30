import { screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { json, signedInAs, signedOut, stubApi } from '../test/fakeApi.ts'
import { renderApp } from '../test/renderApp.tsx'

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('AppRoutes', () => {
  it('sends a signed-out user to the login page', async () => {
    stubApi(signedOut)

    renderApp('/ncrs/new')

    expect(await screen.findByRole('heading', { level: 1, name: 'Sign in' })).toBeInTheDocument()
  })

  it('lets an inspector open the raise form', async () => {
    stubApi(signedInAs('Inspector'))

    renderApp('/ncrs/new')

    expect(await screen.findByRole('heading', { level: 1, name: 'Raise an NCR' })).toBeInTheDocument()
  })

  it.each(['Investigator', 'QaManager'] as const)('tells a %s they cannot raise an NCR', async (role) => {
    stubApi(signedInAs(role))

    renderApp('/ncrs/new')

    expect(await screen.findByRole('heading', { level: 1, name: 'You cannot open this page' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Raise an NCR' })).not.toBeInTheDocument()
  })

  it('shows the raise link only to inspectors', async () => {
    stubApi(signedInAs('Investigator'))

    renderApp('/')

    const navigation = await screen.findByRole('navigation', { name: 'Main' })
    expect(navigation).not.toHaveTextContent('Raise an NCR')
  })

  it('shows who is signed in, with their role in words', async () => {
    stubApi(signedInAs('QaManager'))

    renderApp('/')

    expect(await screen.findByText('qamanager@example.com (QA Manager)')).toBeInTheDocument()
  })

  it('says the service is unavailable when the session check fails, rather than asking to sign in', async () => {
    stubApi({ 'GET /api/auth/me': () => new Response(null, { status: 500 }) })

    renderApp('/')

    expect(await screen.findByRole('alert')).toHaveTextContent('QualiTrack cannot be reached')
    expect(screen.queryByRole('heading', { name: 'Sign in' })).not.toBeInTheDocument()
  })

  it('treats a session answer it does not recognise as a failure, not a signed-in user', async () => {
    stubApi({
      'GET /api/auth/me': () => json({ id: '6f1c2b8e-0000-4000-8000-000000000009', email: 'a@b.c', role: 'Administrator' }),
    })

    renderApp('/')

    expect(await screen.findByRole('alert')).toHaveTextContent('QualiTrack cannot be reached')
  })

  it('shows a not-found page for an unknown address', async () => {
    stubApi(signedInAs('Inspector'))

    renderApp('/no-such-page')

    expect(await screen.findByRole('heading', { level: 1, name: 'Page not found' })).toBeInTheDocument()
  })
})
