import { z } from 'zod'
import { ncrStatusSchema, severitySchema } from '../api/ncrs.ts'

// The list's address and the API's query string are the same parameters (GET /api/ncrs, D1), so a filtered
// list can be bookmarked or shared, and the page passes its address on as it is.

const sortFieldSchema = z.enum(['RaisedAt', 'DueDate'])
const directionSchema = z.enum(['Descending', 'Ascending'])

export type ListFilters = {
  status?: z.infer<typeof ncrStatusSchema>
  severity?: z.infer<typeof severitySchema>
  overdueOnly: boolean
  sortBy: z.infer<typeof sortFieldSchema>
  direction: z.infer<typeof directionSchema>
  page: number
}

// The API's defaults (ListNcrsQuery): newest first, page 1
export const defaultListFilters: ListFilters = {
  overdueOnly: false,
  sortBy: 'RaisedAt',
  direction: 'Descending',
  page: 1,
}

// An unknown value falls back to its default rather than failing: the address is something people edit
const listFiltersSchema = z.object({
  status: ncrStatusSchema.optional().catch(undefined),
  severity: severitySchema.optional().catch(undefined),
  overdueOnly: z
    .enum(['true', 'false'])
    .transform((value) => value === 'true')
    .catch(defaultListFilters.overdueOnly),
  sortBy: sortFieldSchema.catch(defaultListFilters.sortBy),
  direction: directionSchema.catch(defaultListFilters.direction),
  page: z.coerce.number().int().min(1).catch(defaultListFilters.page),
})

export function readListFilters(params: URLSearchParams): ListFilters {
  return listFiltersSchema.parse(Object.fromEntries(params))
}

/** Only what differs from the defaults, always in the same order, so equal filters give equal addresses. */
export function toSearchParams(filters: ListFilters): URLSearchParams {
  const params = new URLSearchParams()
  for (const key of ['status', 'severity', 'overdueOnly', 'sortBy', 'direction', 'page'] as const) {
    const value = filters[key]
    if (value !== undefined && value !== defaultListFilters[key]) {
      params.set(key, String(value))
    }
  }
  return params
}
