import { createContext, useContext } from 'react'
import type { CurrentUser } from '../api/auth.ts'

export type AuthState =
  | { status: 'checking' }
  | { status: 'signedOut' }
  | { status: 'signedIn'; user: CurrentUser }
  | { status: 'unavailable' }

export const AuthContext = createContext<AuthState | undefined>(undefined)

export function useAuth(): AuthState {
  const auth = useContext(AuthContext)
  if (!auth) {
    throw new Error('useAuth must be used inside AuthProvider')
  }
  return auth
}
