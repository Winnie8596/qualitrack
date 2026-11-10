import { describe, expect, it } from 'vitest'
import { ApiError } from './client.ts'
import { shouldRetry } from './queryClient.ts'

describe('shouldRetry', () => {
  it.each([400, 401, 403, 404, 409])('does not retry a %i, because asking again gets the same answer', (status) => {
    expect(shouldRetry(0, new ApiError(status, {}))).toBe(false)
  })

  it('retries a server error twice', () => {
    const error = new ApiError(503, {})

    expect([0, 1, 2].map((failures) => shouldRetry(failures, error))).toEqual([true, true, false])
  })

  it('retries a network failure', () => {
    expect(shouldRetry(0, new TypeError('Failed to fetch'))).toBe(true)
  })
})
