import { vi } from 'vitest'
import type { Role } from '../api/auth.ts'

export type FakeRequest = {
  body: unknown
  headers: Headers
}

// Keyed by "METHOD /path", e.g. "POST /api/ncrs"
export type FakeRoutes = Record<string, (request: FakeRequest) => Response>

export function json(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

export function problem(status: number, details: Record<string, unknown>) {
  return new Response(JSON.stringify({ status, ...details }), {
    status,
    headers: { 'Content-Type': 'application/problem+json' },
  })
}

export const userIds = {
  inspector: '6f1c2b8e-0000-4000-8000-000000000001',
  investigator: '6f1c2b8e-0000-4000-8000-000000000002',
  qaManager: '6f1c2b8e-0000-4000-8000-000000000003',
} as const

const idOfRole: Record<Role, string> = {
  Inspector: userIds.inspector,
  Investigator: userIds.investigator,
  QaManager: userIds.qaManager,
}

export function currentUser(role: Role) {
  return { id: idOfRole[role], email: `${role.toLowerCase()}@example.com`, role }
}

export function signedInAs(role: Role): FakeRoutes {
  return { 'GET /api/auth/me': () => json(currentUser(role)) }
}

export const signedOut: FakeRoutes = {
  'GET /api/auth/me': () => problem(401, { title: 'Unauthorized' }),
}

/**
 * Replaces fetch with the given routes, plus /health and an antiforgery token that every page may ask for.
 * A request with no route fails the way a network error does, so a missing fake shows up in the test.
 */
export function stubApi(routes: FakeRoutes) {
  const all: FakeRoutes = {
    'GET /health': () => new Response(null),
    'GET /api/auth/antiforgery': () => json({ token: 'test-token' }),
    ...routes,
  }

  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const key = `${init?.method ?? 'GET'} ${String(input)}`
    const route = all[key]
    if (!route) {
      throw new TypeError(`No fake API route for ${key}`)
    }
    return route({
      body: typeof init?.body === 'string' ? JSON.parse(init.body) : undefined,
      headers: new Headers(init?.headers),
    })
  })
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

/** The requests made to one route, as "METHOD /path". */
export function callsTo(fetchMock: ReturnType<typeof stubApi>, key: string): FakeRequest[] {
  return fetchMock.mock.calls
    .filter(([input, init]) => `${init?.method ?? 'GET'} ${String(input)}` === key)
    .map(([, init]) => ({
      body: typeof init?.body === 'string' ? JSON.parse(init.body) : undefined,
      headers: new Headers(init?.headers),
    }))
}
