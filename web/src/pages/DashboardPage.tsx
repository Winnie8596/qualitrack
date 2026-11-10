import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { dashboardQueryOptions } from '../api/dashboard.ts'
import FormAlert from '../forms/FormAlert.tsx'
import { defaultListFilters, toSearchParams } from '../ncrs/listFilters.ts'
import SeverityBadge from '../ncrs/SeverityBadge.tsx'

// The list, filtered to what the overdue figure counts, most urgent first
const overdueListPath = `/ncrs?${toSearchParams({ ...defaultListFilters, overdueOnly: true, sortBy: 'DueDate', direction: 'Ascending' })}`

const averageFormat = new Intl.NumberFormat(undefined, { maximumFractionDigits: 1 })

const tileClass = 'rounded border border-slate-200 bg-white p-4'
const figureClass = 'mt-1 text-3xl font-semibold tabular-nums'

/** The QA manager's numbers (D3). They may be up to a minute old: the API caches them. */
function DashboardPage() {
  const { data, error, isPending } = useQuery(dashboardQueryOptions)

  return (
    <div className="space-y-8">
      <h1 className="text-2xl font-semibold">Dashboard</h1>

      {isPending ? (
        <p role="status">Loading the dashboard…</p>
      ) : error ? (
        <FormAlert message="The dashboard could not be loaded. Try again in a few minutes." />
      ) : (
        <>
          <section aria-labelledby="dashboard-severity">
            <h2 id="dashboard-severity" className="text-lg font-semibold">
              Unfinished NCRs by severity
            </h2>
            {/* A description list: each figure is read with what it counts */}
            <dl className="mt-3 grid gap-4 sm:grid-cols-3">
              {data.unfinishedBySeverity.map(({ severity, count }) => (
                <div key={severity} className={tileClass}>
                  <dt>
                    <SeverityBadge severity={severity} />
                  </dt>
                  <dd className={figureClass}>{count}</dd>
                </div>
              ))}
            </dl>
          </section>

          <dl className="grid gap-4 sm:grid-cols-2">
            <div className={tileClass}>
              <dt className="font-medium">Overdue</dt>
              <dd className={figureClass}>{data.overdueCount}</dd>
              <dd className="mt-2">
                <Link to={overdueListPath} className="text-blue-800 underline underline-offset-4">
                  See the overdue NCRs
                </Link>
              </dd>
            </div>
            <div className={tileClass}>
              <dt className="font-medium">Average days to close</dt>
              {data.averageDaysToClose === null ? (
                <dd className="mt-2 text-slate-700">No NCRs closed in the last 90 days</dd>
              ) : (
                <>
                  <dd className={figureClass}>{averageFormat.format(data.averageDaysToClose)}</dd>
                  <dd className="mt-1 text-sm text-slate-600">From raised to closed, last 90 days</dd>
                </>
              )}
            </div>
          </dl>
        </>
      )}
    </div>
  )
}

export default DashboardPage
