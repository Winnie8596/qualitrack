import { useQuery } from '@tanstack/react-query'
import { ApiError } from '../api/client.ts'
import { ncrQueryOptions } from '../api/ncrs.ts'
import FormAlert from '../forms/FormAlert.tsx'
import DateTime from './DateTime.tsx'
import DetailRow from './DetailRow.tsx'
import { statusLabels } from './labels.ts'
import NcrNotFound from './NcrNotFound.tsx'
import OverdueBadge from './OverdueBadge.tsx'
import SeverityBadge from './SeverityBadge.tsx'
import StatusBadge from './StatusBadge.tsx'

type NcrDetailProps = {
  id: string
  currentUserId: string
}

// The API returns users as ids only. Until it returns names, the reader is "You" and anyone else a short id.
function describeUser(userId: string, currentUserId: string): string {
  return userId === currentUserId ? 'You' : `User ${userId.slice(0, 8)}`
}

const sectionHeadingClass = 'text-lg font-semibold'

/** Everything known about one NCR (D2, without the photo until S7). */
function NcrDetail({ id, currentUserId }: NcrDetailProps) {
  const { data: ncr, error, isPending } = useQuery(ncrQueryOptions(id))

  if (isPending) {
    return <p role="status">Loading the NCR…</p>
  }
  if (error instanceof ApiError && error.status === 404) {
    return <NcrNotFound />
  }
  if (error) {
    return <FormAlert message="This NCR could not be loaded. Try again in a few minutes." />
  }

  return (
    <article className="space-y-8">
      <h1 className="text-2xl font-semibold">
        {ncr.number}: {ncr.title}
      </h1>

      <section aria-labelledby="ncr-details">
        <h2 id="ncr-details" className={sectionHeadingClass}>
          Details
        </h2>
        <dl className="mt-2 divide-y divide-slate-200">
          <DetailRow term="Status">
            <StatusBadge status={ncr.status} />
          </DetailRow>
          <DetailRow term="Severity">
            <SeverityBadge severity={ncr.severity} />
          </DetailRow>
          <DetailRow term="Description">
            <span className="whitespace-pre-line">{ncr.description}</span>
          </DetailRow>
          <DetailRow term="Location">{ncr.location}</DetailRow>
          <DetailRow term="Raised by">{describeUser(ncr.raisedBy, currentUserId)}</DetailRow>
          <DetailRow term="Raised">
            <DateTime iso={ncr.raisedAt} />
          </DetailRow>
          <DetailRow term="Investigator">
            {ncr.investigatorId ? describeUser(ncr.investigatorId, currentUserId) : 'Not assigned yet'}
          </DetailRow>
          <DetailRow term="Due">
            {ncr.dueDate ? <DateTime iso={ncr.dueDate} /> : 'Not set'}
            {ncr.isOverdue && (
              <span className="ml-2">
                <OverdueBadge />
              </span>
            )}
          </DetailRow>
          <DetailRow term="Root cause">
            {ncr.rootCause ? <span className="whitespace-pre-line">{ncr.rootCause}</span> : 'Not recorded yet'}
          </DetailRow>
        </dl>
      </section>

      <section aria-labelledby="ncr-actions">
        <h2 id="ncr-actions" className={sectionHeadingClass}>
          Corrective actions
        </h2>
        {ncr.actions.length === 0 ? (
          <p className="mt-2 text-slate-700">No corrective actions yet.</p>
        ) : (
          <ul className="mt-2 space-y-2">
            {ncr.actions.map((action) => (
              <li key={action.id}>
                {action.description}{' '}
                <span className="text-sm text-slate-600">
                  {action.completedAt ? (
                    <>
                      (completed <DateTime iso={action.completedAt} />)
                    </>
                  ) : (
                    '(not completed)'
                  )}
                </span>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section aria-labelledby="ncr-history">
        <h2 id="ncr-history" className={sectionHeadingClass}>
          History
        </h2>
        {ncr.history.length === 0 ? (
          <p className="mt-2 text-slate-700">No status changes yet.</p>
        ) : (
          <ol className="mt-2 space-y-2">
            {ncr.history.map((change) => (
              <li key={`${change.at}-${change.to}`}>
                <span className="font-medium">
                  {statusLabels[change.from]} → {statusLabels[change.to]}
                </span>{' '}
                <span className="text-sm text-slate-600">
                  by {describeUser(change.by, currentUserId)}, <DateTime iso={change.at} />
                </span>
                {change.note && <p className="text-slate-700">{change.note}</p>}
              </li>
            ))}
          </ol>
        )}
      </section>
    </article>
  )
}

export default NcrDetail
