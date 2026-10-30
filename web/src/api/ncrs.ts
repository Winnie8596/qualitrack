import { queryOptions } from '@tanstack/react-query'
import { z } from 'zod'
import { getJson, postJson } from './client.ts'

// The API sends enums by name (JsonStringEnumConverter), so these match the C# enums one for one.
export const severitySchema = z.enum(['Minor', 'Major', 'Critical'])

export type Severity = z.infer<typeof severitySchema>

export const ncrStatusSchema = z.enum([
  'Open',
  'Investigating',
  'ActionInProgress',
  'PendingVerification',
  'Closed',
  'Cancelled',
])

export type NcrStatus = z.infer<typeof ncrStatusSchema>

const timestamp = z.iso.datetime({ offset: true })

const correctiveActionSchema = z.object({
  id: z.guid(),
  description: z.string(),
  addedBy: z.guid(),
  addedAt: timestamp,
  completedBy: z.guid().nullable(),
  completedAt: timestamp.nullable(),
})

const statusChangeSchema = z.object({
  from: ncrStatusSchema,
  to: ncrStatusSchema,
  by: z.guid(),
  at: timestamp,
  note: z.string().nullable(),
})

// GET /api/ncrs/{id}: the API's NcrView
const ncrSchema = z.object({
  id: z.guid(),
  number: z.string(),
  title: z.string(),
  description: z.string(),
  location: z.string(),
  severity: severitySchema,
  status: ncrStatusSchema,
  raisedBy: z.guid(),
  raisedAt: timestamp,
  investigatorId: z.guid().nullable(),
  dueDate: timestamp.nullable(),
  isOverdue: z.boolean(),
  rootCause: z.string().nullable(),
  actions: z.array(correctiveActionSchema),
  history: z.array(statusChangeSchema),
})

export type Ncr = z.infer<typeof ncrSchema>

export type RaiseNcrRequest = {
  title: string
  description: string
  location: string
  severity: Severity
}

const raisedNcrSchema = z.object({ id: z.guid() })

export function ncrQueryOptions(id: string) {
  return queryOptions({
    queryKey: ['ncrs', id],
    queryFn: ({ signal }) => getJson(`/api/ncrs/${encodeURIComponent(id)}`, ncrSchema, signal),
  })
}

/** Raises an NCR and returns its id, for the detail page to open. */
export async function raiseNcr(request: RaiseNcrRequest): Promise<string> {
  const { id } = await postJson('/api/ncrs', request, raisedNcrSchema)
  return id
}
