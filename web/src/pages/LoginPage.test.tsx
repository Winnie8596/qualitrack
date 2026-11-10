import { screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { callsTo, currentUser, json, problem, signedInAs, signedOut, stubApi } from '../test/fakeApi.ts'
import { renderApp } from '../test/renderApp.tsx'

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('LoginPage', () => {
  it('asks for both fields before sending anything', async () => {
    const fetchMock = stubApi(signedOut)
    const { user } = renderApp('/login')

    await user.click(await screen.findByRole('button', { name: 'Sign in' }))

    expect(screen.getByLabelText('Email')).toHaveAccessibleDescription('Enter your email.')
    expect(screen.getByLabelText('Password')).toHaveAccessibleDescription('Enter your password.')
    expect(screen.getByLabelText('Email')).toHaveAttribute('aria-invalid', 'true')
    expect(callsTo(fetchMock, 'POST /api/auth/login')).toHaveLength(0)
  })

  it('signs in and returns the user to the page they were trying to open', async () => {
    const fetchMock = stubApi({ ...signedOut, 'POST /api/auth/login': () => json(currentUser('Inspector')) })
    const { user } = renderApp('/ncrs/new')

    await user.type(await screen.findByLabelText('Email'), 'inspector@example.com')
    await user.type(screen.getByLabelText('Password'), 'Secret-123')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'Raise an NCR' })).toBeInTheDocument()
    expect(screen.getByText('inspector@example.com (Inspector)')).toBeInTheDocument()
    expect(callsTo(fetchMock, 'POST /api/auth/login')[0].body).toEqual({
      email: 'inspector@example.com',
      password: 'Secret-123',
    })
  })

  it('goes to the home page after signing in from the login page itself', async () => {
    stubApi({ ...signedOut, 'POST /api/auth/login': () => json(currentUser('QaManager')) })
    const { user } = renderApp('/login')

    await user.type(await screen.findByLabelText('Email'), 'qamanager@example.com')
    await user.type(screen.getByLabelText('Password'), 'Secret-123')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'QualiTrack' })).toBeInTheDocument()
  })

  it('shows the reason a sign-in was refused', async () => {
    stubApi({ ...signedOut, 'POST /api/auth/login': () => problem(401, { title: 'Email or password is wrong.' }) })
    const { user } = renderApp('/login')

    await user.type(await screen.findByLabelText('Email'), 'inspector@example.com')
    await user.type(screen.getByLabelText('Password'), 'wrong')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Email or password is wrong.')
    expect(screen.getByRole('heading', { level: 1, name: 'Sign in' })).toBeInTheDocument()
  })

  it('sends a user who is already signed in to the home page', async () => {
    stubApi(signedInAs('Inspector'))

    renderApp('/login')

    expect(await screen.findByRole('heading', { level: 1, name: 'QualiTrack' })).toBeInTheDocument()
  })
})

describe('Signing out', () => {
  it('ends the session and shows the login page', async () => {
    const fetchMock = stubApi({ ...signedInAs('Inspector'), 'POST /api/auth/logout': () => new Response(null, { status: 204 }) })
    const { user } = renderApp('/')

    await user.click(await screen.findByRole('button', { name: 'Sign out' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'Sign in' })).toBeInTheDocument()
    expect(callsTo(fetchMock, 'POST /api/auth/logout')).toHaveLength(1)
    expect(screen.queryByRole('navigation', { name: 'Main' })).not.toBeInTheDocument()
  })
})
