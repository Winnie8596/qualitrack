import { QueryClient } from '@tanstack/react-query'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { forgetAntiforgeryToken } from './client.ts'
import { ncrQueryOptions, raiseNcr } from './ncrs.ts'

function json(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

// An NcrView as System.Text.Json writes it: camelCase names, enums by name, seven-digit fractions, nulls kept
const investigatingNcr = {
  id: '0199a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a5b',
  number: 'NCR-2026-0001',
  title: 'Weld crack on bracket',
  description: 'Hairline crack along the fillet weld.',
  location: 'Bay 3',
  severity: 'Critical',
  status: 'Investigating',
  raisedBy: '6f1c2b8e-0000-4000-8000-000000000001',
  raisedAt: '2026-10-30T08:15:00.1234567+00:00',
  investigatorId: '6f1c2b8e-0000-4000-8000-000000000002',
  dueDate: '2026-11-05T00:00:00+00:00',
  isOverdue: false,
  rootCause: null,
  actions: [],
  history: [
    {
      from: 'Open',
      to: 'Investigating',
      by: '6f1c2b8e-0000-4000-8000-000000000003',
      at: '2026-10-30T09:00:00+00:00',
      note: null,
    },
  ],
}

afterEach(() => {
  vi.unstubAllGlobals()
  forgetAntiforgeryToken()
})

describe('ncrQueryOptions', () => {
  it('reads an NCR in the shape the API sends it', async () => {
    const fetchMock = vi.fn(async (_input: RequestInfo | URL) => json(investigatingNcr))
    vi.stubGlobal('fetch', fetchMock)

    const ncr = await new QueryClient().fetchQuery(ncrQueryOptions(investigatingNcr.id))

    expect(ncr).toEqual(investigatingNcr)
    expect(String(fetchMock.mock.calls[0][0])).toBe(`/api/ncrs/${investigatingNcr.id}`)
  })
})

describe('raiseNcr', () => {
  it('posts the form to /api/ncrs and returns the new id', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) =>
      String(input) === '/api/auth/antiforgery' ? json({ token: 'tok' }) : json({ id: investigatingNcr.id }, 201),
    )
    vi.stubGlobal('fetch', fetchMock)

    const id = await raiseNcr({ title: 'Weld crack', description: 'Crack', location: 'Bay 3', severity: 'Major' })

    expect(id).toBe(investigatingNcr.id)
    expect(fetchMock.mock.calls.map(([url]) => String(url))).toEqual(['/api/auth/antiforgery', '/api/ncrs'])
  })
})
