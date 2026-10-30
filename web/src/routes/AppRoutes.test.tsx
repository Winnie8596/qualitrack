import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import AppRoutes from './AppRoutes.tsx'

function json(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

// The API as the app sees it: /api/auth/me answers with the given response; /health is always up.
function serverWhereMeReturns(meResponse: () => Response) {
  vi.stubGlobal(
    'fetch',
    vi.fn(async (input: RequestInfo | URL) => (String(input) === '/api/auth/me' ? meResponse() : new Response(null))),
  )
}

function signedInAs(role: string) {
  serverWhereMeReturns(() => json({ id: '6f1c2b8e-0000-4000-8000-000000000001', email: 'user@example.com', role }))
}

function renderAt(path: string) {
  // A fresh cache per test, with no retries, so a failure shows at once
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[path]}>
        <AppRoutes />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('AppRoutes', () => {
  it('sends a signed-out user to the login page', async () => {
    serverWhereMeReturns(() => json({ title: 'Unauthorized' }, 401))

    renderAt('/ncrs/new')

    expect(await screen.findByRole('heading', { level: 1, name: 'Sign in' })).toBeInTheDocument()
  })

  it('lets an inspector open the raise form', async () => {
    signedInAs('Inspector')

    renderAt('/ncrs/new')

    expect(await screen.findByRole('heading', { level: 1, name: 'Raise an NCR' })).toBeInTheDocument()
  })

  it.each(['Investigator', 'QaManager'])('tells a %s they cannot raise an NCR', async (role) => {
    signedInAs(role)

    renderAt('/ncrs/new')

    expect(await screen.findByRole('heading', { level: 1, name: 'You cannot open this page' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Raise an NCR' })).not.toBeInTheDocument()
  })

  it('shows the raise link only to inspectors', async () => {
    signedInAs('Investigator')

    renderAt('/')

    const navigation = await screen.findByRole('navigation', { name: 'Main' })
    expect(navigation).not.toHaveTextContent('Raise an NCR')
  })

  it('shows who is signed in, with their role in words', async () => {
    signedInAs('QaManager')

    renderAt('/')

    expect(await screen.findByText('user@example.com (QA Manager)')).toBeInTheDocument()
  })

  it('says the service is unavailable when the session check fails, rather than asking to sign in', async () => {
    serverWhereMeReturns(() => new Response(null, { status: 500 }))

    renderAt('/')

    expect(await screen.findByRole('alert')).toHaveTextContent('QualiTrack cannot be reached')
    expect(screen.queryByRole('heading', { name: 'Sign in' })).not.toBeInTheDocument()
  })

  it('treats a session answer it does not recognise as a failure, not a signed-in user', async () => {
    signedInAs('Administrator')

    renderAt('/')

    expect(await screen.findByRole('alert')).toHaveTextContent('QualiTrack cannot be reached')
  })

  it('shows a not-found page for an unknown address', async () => {
    signedInAs('Inspector')

    renderAt('/no-such-page')

    expect(await screen.findByRole('heading', { level: 1, name: 'Page not found' })).toBeInTheDocument()
  })
})
