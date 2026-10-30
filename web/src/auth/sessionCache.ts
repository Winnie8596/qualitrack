import type { QueryClient } from '@tanstack/react-query'
import { sessionQueryOptions, type CurrentUser, type SessionCheck } from '../api/auth.ts'
import { forgetAntiforgeryToken } from '../api/client.ts'

// The antiforgery token belongs to one user, so it goes whenever the user changes.

export function rememberSignedIn(queryClient: QueryClient, user: CurrentUser): void {
  forgetAntiforgeryToken()
  // Typed first: passed inline, TypeScript 7 checks the literal against the wrong member of setQueryData's updater
  const session: SessionCheck = { kind: 'signedIn', user }
  queryClient.setQueryData(sessionQueryOptions.queryKey, session)
}

/** Drops everything cached for the user who left, so the next one on this browser never sees it. */
export function rememberSignedOut(queryClient: QueryClient): void {
  forgetAntiforgeryToken()
  queryClient.removeQueries({ predicate: (query) => query.queryKey[0] !== sessionQueryOptions.queryKey[0] })
  queryClient.setQueryData(sessionQueryOptions.queryKey, { kind: 'signedOut' })
}
