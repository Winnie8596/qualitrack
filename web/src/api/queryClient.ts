import { QueryClient } from '@tanstack/react-query'
import { ApiError } from './client.ts'

const maxRetries = 2

/** A 4xx is the API's answer and asking again gets the same one; server errors and network blips may pass. */
export function shouldRetry(failureCount: number, error: unknown): boolean {
  if (error instanceof ApiError && error.status < 500) {
    return false
  }
  return failureCount < maxRetries
}

export function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: shouldRetry },
    },
  })
}
