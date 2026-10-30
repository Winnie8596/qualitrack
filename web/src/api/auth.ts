import { z } from 'zod'
import { ApiError, getJson } from './client.ts'

// Matches the API's Role enum names, which /api/auth/me sends as text.
export const roleSchema = z.enum(['Inspector', 'Investigator', 'QaManager'])

export type Role = z.infer<typeof roleSchema>

const currentUserSchema = z.object({
  id: z.guid(),
  email: z.string(),
  role: roleSchema,
})

export type CurrentUser = z.infer<typeof currentUserSchema>

export type SessionCheck = { kind: 'signedIn'; user: CurrentUser } | { kind: 'signedOut' }

export const sessionQueryKey = ['session'] as const

/**
 * Asks the API who the session cookie belongs to. A 401 is an answer (nobody is signed in); any other failure is
 * thrown, so a broken server is never mistaken for a signed-out user.
 */
export async function checkSession(signal: AbortSignal): Promise<SessionCheck> {
  try {
    return { kind: 'signedIn', user: await getJson('/api/auth/me', currentUserSchema, signal) }
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      return { kind: 'signedOut' }
    }
    throw error
  }
}
