import { Navigate, Outlet, useLocation } from 'react-router'
import type { Role } from '../api/auth.ts'
import { useAuth } from './useAuth.ts'

type ProtectedRouteProps = {
  // Leave out to let any signed-in user in.
  allow?: readonly Role[]
}

/**
 * Keeps pages away from users who cannot use them. This is for the user's benefit only: the API checks the session
 * and role on every request, so a page shown by mistake still cannot read or change anything.
 */
function ProtectedRoute({ allow }: ProtectedRouteProps) {
  const auth = useAuth()
  const location = useLocation()

  switch (auth.status) {
    case 'checking':
      return <p role="status">Checking your session…</p>
    case 'unavailable':
      return (
        <p role="alert" className="rounded border border-red-300 bg-red-50 px-4 py-3 text-red-900">
          QualiTrack cannot be reached right now. Try again in a few minutes.
        </p>
      )
    case 'signedOut':
      // Remembers where the user was going, so login can send them back there.
      return <Navigate to="/login" replace state={{ from: location }} />
    case 'signedIn':
      if (allow && !allow.includes(auth.user.role)) {
        return (
          <>
            <h1 className="text-2xl font-semibold">You cannot open this page</h1>
            <p className="mt-2 text-slate-700">Your role does not have access to it.</p>
          </>
        )
      }
      return <Outlet />
  }
}

export default ProtectedRoute
