// Matches the API's Role enum names, which /api/auth/me sends as text.
export const roles = ['Inspector', 'Investigator', 'QaManager'] as const

export type Role = (typeof roles)[number]

export type CurrentUser = {
  id: string
  email: string
  role: Role
}

export type SessionCheck = { kind: 'signedIn'; user: CurrentUser } | { kind: 'signedOut' }

function isCurrentUser(value: unknown): value is CurrentUser {
  if (typeof value !== 'object' || value === null) {
    return false
  }
  const { id, email, role } = value as Record<string, unknown>
  return typeof id === 'string' && typeof email === 'string' && roles.some((known) => known === role)
}

/**
 * Asks the API who the session cookie belongs to. A 401 is an answer (nobody is signed in); anything else that is
 * not a valid user is a failure, so a broken server is never mistaken for a signed-out user.
 */
export async function checkSession(signal: AbortSignal): Promise<SessionCheck> {
  const response = await fetch('/api/auth/me', { signal })
  if (response.status === 401) {
    return { kind: 'signedOut' }
  }
  if (!response.ok) {
    throw new Error(`Session check failed with status ${response.status}`)
  }

  const body: unknown = await response.json()
  if (!isCurrentUser(body)) {
    throw new Error('Session check returned an unexpected body')
  }
  return { kind: 'signedIn', user: body }
}
