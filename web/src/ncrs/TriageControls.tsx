import type { Ncr } from '../api/ncrs.ts'
import AssignForm from './AssignForm.tsx'
import CancelForm from './CancelForm.tsx'

type TriageControlsProps = {
  ncr: Ncr
}

/**
 * What a QA manager can do to an NCR (A2, A3). The API enforces the same rules; this only hides what would be
 * refused, so the caller decides who sees it and this decides when.
 */
function TriageControls({ ncr }: TriageControlsProps) {
  const isFinished = ncr.status === 'Closed' || ncr.status === 'Cancelled'
  if (isFinished) {
    return null
  }

  return (
    <section aria-labelledby="ncr-triage" className="space-y-6">
      <h2 id="ncr-triage" className="text-lg font-semibold">
        Triage
      </h2>
      <div className="grid gap-8 md:grid-cols-2">
        {ncr.status === 'Open' && <AssignForm ncrId={ncr.id} />}
        <CancelForm ncrId={ncr.id} />
      </div>
    </section>
  )
}

export default TriageControls
