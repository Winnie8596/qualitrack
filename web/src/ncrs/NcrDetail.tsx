import { useQuery } from '@tanstack/react-query'
import type { CurrentUser } from '../api/auth.ts'
import { ApiError } from '../api/client.ts'
import { ncrQueryOptions, photoUrl } from '../api/ncrs.ts'
import FormAlert from '../forms/FormAlert.tsx'
import CorrectiveActions from './CorrectiveActions.tsx'
import DateTime from './DateTime.tsx'
import { describeUser } from './describeUser.ts'
import DetailRow from './DetailRow.tsx'
import HistoryTimeline from './HistoryTimeline.tsx'
import InvestigationControls from './InvestigationControls.tsx'
import NcrNotFound from './NcrNotFound.tsx'
import OverdueBadge from './OverdueBadge.tsx'
import { isAssignedInvestigator, isQaManager } from './permissions.ts'
import SeverityBadge from './SeverityBadge.tsx'
import StatusBadge from './StatusBadge.tsx'
import TriageControls from './TriageControls.tsx'
import VerificationControls from './VerificationControls.tsx'

type NcrDetailProps = {
  id: string
  currentUser: CurrentUser
  // Set right after raising, when the NCR was saved but its photo was refused or lost on the way
  photoNotAttached?: boolean
}

const sectionHeadingClass = 'text-lg font-semibold'

/** Everything known about one NCR (D2, without the photo until S7). */
function NcrDetail({ id, currentUser, photoNotAttached = false }: NcrDetailProps) {
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
      {photoNotAttached && (
        <FormAlert message="The NCR was raised, but its photo could not be attached. Check it is a JPEG or PNG of 5 MB or less." />
      )}

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
          <DetailRow term="Raised by">{describeUser(ncr.raisedBy, currentUser.id)}</DetailRow>
          <DetailRow term="Raised">
            <DateTime iso={ncr.raisedAt} />
          </DetailRow>
          <DetailRow term="Investigator">
            {ncr.investigatorId ? describeUser(ncr.investigatorId, currentUser.id) : 'Not assigned yet'}
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

      {isQaManager(currentUser) && <VerificationControls ncr={ncr} />}

      {isQaManager(currentUser) && <TriageControls ncr={ncr} />}

      {ncr.hasPhoto && (
        <section aria-labelledby="ncr-photo">
          <h2 id="ncr-photo" className={sectionHeadingClass}>
            Photo
          </h2>
          <img
            src={photoUrl(ncr.id)}
            alt={`Photo of ${ncr.title}`}
            className="mt-2 max-h-96 w-auto max-w-full rounded border border-slate-200"
          />
        </section>
      )}

      {isAssignedInvestigator(ncr, currentUser) && <InvestigationControls ncr={ncr} />}

      <CorrectiveActions ncr={ncr} currentUser={currentUser} />

      <HistoryTimeline history={ncr.history} currentUserId={currentUser.id} />
    </article>
  )
}

export default NcrDetail
