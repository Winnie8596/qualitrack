import { queryOptions } from '@tanstack/react-query'
import { z } from 'zod'
import { getJson } from './client.ts'

const userSchema = z.object({ id: z.guid(), email: z.string() })

export type User = z.infer<typeof userSchema>

/** GET /api/users/investigators: who a QA manager can assign an NCR to, sorted by email. */
export const investigatorsQueryOptions = queryOptions({
  queryKey: ['users', 'investigators'],
  queryFn: ({ signal }) => getJson('/api/users/investigators', z.array(userSchema), signal),
})
