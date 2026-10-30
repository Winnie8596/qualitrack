import { useQuery } from '@tanstack/react-query'
import { checkSession, sessionQueryKey, type CurrentUser } from '../api/auth.ts'

export type AuthState =
  | { status: 'checking' }
  | { status: 'signedOut' }
  | { status: 'signedIn'; user: CurrentUser }
  | { status: 'unavailable' }

// Long enough that moving between pages does not ask again; short enough that an expired session is noticed.
const sessionStaleTime = 5 * 60 * 1000

/**
 * Who is signed in, from /api/auth/me. The cookie is HttpOnly, so the app cannot read it; it asks the API, and the
 * query cache shares the answer with every component that calls this.
 */
export function useAuth(): AuthState {
  const session = useQuery({
    queryKey: sessionQueryKey,
    queryFn: ({ signal }) => checkSession(signal),
    staleTime: sessionStaleTime,
  })

  // A known answer wins over a failed refresh of it: one dropped request should not sign the page out
  if (session.data) {
    return session.data.kind === 'signedIn' ? { status: 'signedIn', user: session.data.user } : { status: 'signedOut' }
  }
  return session.isError ? { status: 'unavailable' } : { status: 'checking' }
}
