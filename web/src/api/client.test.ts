import { afterEach, describe, expect, it, vi } from 'vitest'
import { z } from 'zod'
import { ApiError, forgetAntiforgeryToken, getJson, postJson } from './client.ts'

const thing = z.object({ id: z.string() })

function json(body: unknown, status = 200, contentType = 'application/json') {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}

function stubFetch(respond: (url: string, init?: RequestInit) => Response) {
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => respond(String(input), init))
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

afterEach(() => {
  vi.unstubAllGlobals()
  forgetAntiforgeryToken()
})

describe('getJson', () => {
  it('returns the body when it matches the schema', async () => {
    stubFetch(() => json({ id: 'abc' }))

    const result = await getJson('/api/things/abc', thing)

    expect(result).toEqual({ id: 'abc' })
  })

  it('throws an ApiError carrying the ProblemDetails of a failed request', async () => {
    stubFetch(() => json({ title: 'Not Found', status: 404, detail: 'NCR 1 was not found.' }, 404, 'application/problem+json'))

    const failure = getJson('/api/things/1', thing)

    await expect(failure).rejects.toBeInstanceOf(ApiError)
    await expect(failure).rejects.toMatchObject({ status: 404, problem: { detail: 'NCR 1 was not found.' } })
  })

  it('still throws an ApiError with the status when the error body is not JSON', async () => {
    stubFetch(() => new Response('Bad gateway', { status: 502 }))

    const failure = getJson('/api/things/1', thing)

    await expect(failure).rejects.toMatchObject({ status: 502, problem: {} })
  })

  it('rejects a successful response whose body breaks the contract', async () => {
    stubFetch(() => json({ id: 42 }))

    const failure = getJson('/api/things/1', thing)

    await expect(failure).rejects.toThrow()
    await expect(failure).rejects.not.toBeInstanceOf(ApiError)
  })

  it('does not ask for an antiforgery token, because reads change nothing', async () => {
    const fetchMock = stubFetch(() => json({ id: 'abc' }))

    await getJson('/api/things/abc', thing)

    expect(fetchMock).toHaveBeenCalledTimes(1)
  })
})

describe('postJson', () => {
  it('sends the body as JSON with the antiforgery token, fetching the token once', async () => {
    const fetchMock = stubFetch((url) => (url === '/api/auth/antiforgery' ? json({ token: 'tok-1' }) : json({ id: 'new' }, 201)))

    await postJson('/api/things', { name: 'first' }, thing)
    const second = await postJson('/api/things', { name: 'second' }, thing)

    expect(second).toEqual({ id: 'new' })
    const writes = fetchMock.mock.calls.filter(([url]) => String(url) === '/api/things')
    expect(writes).toHaveLength(2)
    const [, init] = writes[1]
    expect(init?.method).toBe('POST')
    expect(init?.body).toBe(JSON.stringify({ name: 'second' }))
    expect(new Headers(init?.headers).get('X-XSRF-TOKEN')).toBe('tok-1')
    expect(new Headers(init?.headers).get('Content-Type')).toBe('application/json')
    expect(fetchMock.mock.calls.filter(([url]) => String(url) === '/api/auth/antiforgery')).toHaveLength(1)
  })

  it('asks for a new token after the old one is forgotten, as after signing in or out', async () => {
    let issued = 0
    const fetchMock = stubFetch((url) =>
      url === '/api/auth/antiforgery' ? json({ token: `tok-${++issued}` }) : json({ id: 'new' }, 201),
    )

    await postJson('/api/things', {}, thing)
    forgetAntiforgeryToken()
    await postJson('/api/things', {}, thing)

    const [, init] = fetchMock.mock.calls.filter(([url]) => String(url) === '/api/things')[1]
    expect(new Headers(init?.headers).get('X-XSRF-TOKEN')).toBe('tok-2')
  })

  it('tries for a token again after failing to get one', async () => {
    let tokenRequests = 0
    stubFetch((url) => {
      if (url === '/api/auth/antiforgery') {
        tokenRequests++
        return tokenRequests === 1 ? new Response(null, { status: 503 }) : json({ token: 'tok' })
      }
      return json({ id: 'new' }, 201)
    })

    await expect(postJson('/api/things', {}, thing)).rejects.toMatchObject({ status: 503 })
    const retried = await postJson('/api/things', {}, thing)

    expect(retried).toEqual({ id: 'new' })
  })
})
