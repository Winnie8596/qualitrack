import { queryOptions } from '@tanstack/react-query'
import { z } from 'zod'
import { getJson } from './client.ts'
import { severitySchema } from './ncrs.ts'

// GET /api/dashboard: the API's DashboardView
const dashboardSchema = z.object({
  unfinishedBySeverity: z.array(z.object({ severity: severitySchema, count: z.number().int() })),
  overdueCount: z.number().int(),
  // null when no NCR closed in the period, which is not the same as closing in zero days
  averageDaysToClose: z.number().nullable(),
})

export type Dashboard = z.infer<typeof dashboardSchema>

export const dashboardQueryOptions = queryOptions({
  queryKey: ['dashboard'],
  queryFn: ({ signal }) => getJson('/api/dashboard', dashboardSchema, signal),
})
