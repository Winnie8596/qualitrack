import { useEffect, useState, type ReactNode } from 'react'
import { checkSession } from '../api/auth.ts'
import { AuthContext, type AuthState } from './authContext.ts'

type AuthProviderProps = {
  children: ReactNode
}

// The cookie is HttpOnly, so the app cannot read it; it asks the API once on load who is signed in.
function AuthProvider({ children }: AuthProviderProps) {
  const [auth, setAuth] = useState<AuthState>({ status: 'checking' })

  useEffect(() => {
    const controller = new AbortController()

    checkSession(controller.signal)
      .then((session) =>
        setAuth(session.kind === 'signedIn' ? { status: 'signedIn', user: session.user } : { status: 'signedOut' }),
      )
      .catch((error: unknown) => {
        // Unmounting aborts the request; that is not a failed check.
        if (!controller.signal.aborted) {
          console.error('Session check failed', error)
          setAuth({ status: 'unavailable' })
        }
      })

    return () => controller.abort()
  }, [])

  return <AuthContext value={auth}>{children}</AuthContext>
}

export default AuthProvider
