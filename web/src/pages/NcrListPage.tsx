import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useId } from 'react'
import { Link, useSearchParams } from 'react-router'
import { z } from 'zod'
import { ncrListQueryOptions, ncrStatusSchema, severitySchema } from '../api/ncrs.ts'
import { fieldClass } from '../forms/fieldClass.ts'
import FormAlert from '../forms/FormAlert.tsx'
import DateTime from '../ncrs/DateTime.tsx'
import { statusLabels } from '../ncrs/labels.ts'
import { readListFilters, toSearchParams, type ListFilters } from '../ncrs/listFilters.ts'
import OverdueBadge from '../ncrs/OverdueBadge.tsx'
import SeverityBadge from '../ncrs/SeverityBadge.tsx'
import StatusBadge from '../ncrs/StatusBadge.tsx'

const sortOptions = [
  { sortBy: 'RaisedAt', direction: 'Descending', label: 'Raised, newest first' },
  { sortBy: 'RaisedAt', direction: 'Ascending', label: 'Raised, oldest first' },
  { sortBy: 'DueDate', direction: 'Ascending', label: 'Due date, soonest first' },
  { sortBy: 'DueDate', direction: 'Descending', label: 'Due date, latest first' },
] as const

// The "Any …" option has the value "", which is no filter
function chosen<T extends string>(schema: z.ZodType<T>, value: string): T | undefined {
  const parsed = schema.safeParse(value)
  return parsed.success ? parsed.data : undefined
}

const pageButtonClass =
  'rounded border border-slate-400 bg-white px-3 py-1.5 font-medium hover:bg-slate-100 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-700 disabled:opacity-50'

/** The NCRs the signed-in user may see (D1); the API decides which those are. */
function NcrListPage() {
  const headingId = useId()
  const [searchParams, setSearchParams] = useSearchParams()
  const filters = readListFilters(searchParams)
  // Kept while the next page loads, so the table does not blank out on every change
  const { data, error, isPending, isPlaceholderData } = useQuery({
    ...ncrListQueryOptions(toSearchParams(filters)),
    placeholderData: keepPreviousData,
  })

  const show = (next: ListFilters) => setSearchParams(toSearchParams(next))
  // A new filter or sort starts again from the first page
  const filterBy = (change: Partial<ListFilters>) => show({ ...filters, ...change, page: 1 })

  return (
    <div className="space-y-6">
      <h1 id={headingId} className="text-2xl font-semibold">
        NCRs
      </h1>

      <form role="search" aria-label="Filter NCRs" className="grid gap-4 sm:grid-cols-4 sm:items-end">
        <label className="block font-medium">
          Status
          <select
            className={fieldClass}
            value={filters.status ?? ''}
            onChange={(event) => filterBy({ status: chosen(ncrStatusSchema, event.target.value) })}
          >
            <option value="">Any status</option>
            {ncrStatusSchema.options.map((status) => (
              <option key={status} value={status}>
                {statusLabels[status]}
              </option>
            ))}
          </select>
        </label>

        <label className="block font-medium">
          Severity
          <select
            className={fieldClass}
            value={filters.severity ?? ''}
            onChange={(event) => filterBy({ severity: chosen(severitySchema, event.target.value) })}
          >
            <option value="">Any severity</option>
            {severitySchema.options.map((severity) => (
              <option key={severity} value={severity}>
                {severity}
              </option>
            ))}
          </select>
        </label>

        <label className="block font-medium">
          Sort by
          <select
            className={fieldClass}
            value={`${filters.sortBy} ${filters.direction}`}
            onChange={(event) => {
              const option = sortOptions.find(({ sortBy, direction }) => `${sortBy} ${direction}` === event.target.value)
              if (option) {
                filterBy({ sortBy: option.sortBy, direction: option.direction })
              }
            }}
          >
            {sortOptions.map(({ sortBy, direction, label }) => (
              <option key={label} value={`${sortBy} ${direction}`}>
                {label}
              </option>
            ))}
          </select>
        </label>

        <label className="flex items-center gap-2 py-2 font-medium">
          <input
            type="checkbox"
            className="size-4 accent-blue-700"
            checked={filters.overdueOnly}
            onChange={(event) => filterBy({ overdueOnly: event.target.checked })}
          />
          Overdue only
        </label>
      </form>

      {isPending ? (
        <p role="status">Loading NCRs…</p>
      ) : error ? (
        <FormAlert message="The NCR list could not be loaded. Try again in a few minutes." />
      ) : data.items.length === 0 ? (
        <p className="text-slate-700">No NCRs match these filters.</p>
      ) : (
        // Scrolls on its own on a narrow screen, so the page itself never scrolls sideways
        <div className="overflow-x-auto rounded border border-slate-200 bg-white" aria-busy={isPlaceholderData}>
          <table aria-labelledby={headingId} className="w-full text-left">
            <thead className="bg-slate-100 text-sm text-slate-700">
              <tr>
                <th scope="col" className="px-3 py-2 font-semibold">Number</th>
                <th scope="col" className="px-3 py-2 font-semibold">Title</th>
                <th scope="col" className="px-3 py-2 font-semibold">Severity</th>
                <th scope="col" className="px-3 py-2 font-semibold">Status</th>
                <th scope="col" className="px-3 py-2 font-semibold">Raised</th>
                <th scope="col" className="px-3 py-2 font-semibold">Due</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200">
              {data.items.map((ncr) => (
                <tr key={ncr.id}>
                  <td className="px-3 py-2 whitespace-nowrap">
                    <Link to={`/ncrs/${ncr.id}`} className="font-medium text-blue-800 underline underline-offset-4">
                      {ncr.number}
                    </Link>
                  </td>
                  <td className="px-3 py-2">{ncr.title}</td>
                  <td className="px-3 py-2">
                    <SeverityBadge severity={ncr.severity} />
                  </td>
                  <td className="px-3 py-2 whitespace-nowrap">
                    <StatusBadge status={ncr.status} />
                  </td>
                  <td className="px-3 py-2 whitespace-nowrap">
                    <DateTime iso={ncr.raisedAt} />
                  </td>
                  <td className="px-3 py-2 whitespace-nowrap">
                    {ncr.dueDate ? <DateTime iso={ncr.dueDate} /> : 'Not set'}
                    {ncr.isOverdue && (
                      <span className="ml-2">
                        <OverdueBadge />
                      </span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {data && (
        <nav aria-label="Pages" className="flex items-center gap-4">
          <button
            type="button"
            className={pageButtonClass}
            disabled={filters.page <= 1}
            onClick={() => show({ ...filters, page: filters.page - 1 })}
          >
            Previous page
          </button>
          <p>Page {filters.page}</p>
          <button
            type="button"
            className={pageButtonClass}
            disabled={!data.hasNextPage || isPlaceholderData}
            onClick={() => show({ ...filters, page: filters.page + 1 })}
          >
            Next page
          </button>
        </nav>
      )}
    </div>
  )
}

export default NcrListPage
