import type { Ncr } from '../api/ncrs.ts'
import AddActionForm from './AddActionForm.tsx'
import PlanFixForm from './PlanFixForm.tsx'
import SubmitForVerificationButton from './SubmitForVerificationButton.tsx'

type InvestigationControlsProps = {
  ncr: Ncr
}

/**
 * What the assigned investigator can do next (B1–B3): plan the fix while investigating, then add actions and
 * submit. The caller decides who sees this; this decides what the NCR's status allows.
 */
function InvestigationControls({ ncr }: InvestigationControlsProps) {
  if (ncr.status !== 'Investigating' && ncr.status !== 'ActionInProgress') {
    return null
  }

  return (
    <section aria-labelledby="ncr-investigation" className="space-y-6">
      <h2 id="ncr-investigation" className="text-lg font-semibold">
        Investigation
      </h2>
      {ncr.status === 'Investigating' ? (
        <PlanFixForm ncrId={ncr.id} />
      ) : (
        <div className="grid gap-8 md:grid-cols-2">
          <AddActionForm ncrId={ncr.id} />
          <SubmitForVerificationButton ncrId={ncr.id} />
        </div>
      )}
    </section>
  )
}

export default InvestigationControls
