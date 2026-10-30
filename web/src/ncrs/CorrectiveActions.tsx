import type { CurrentUser } from '../api/auth.ts'
import type { Ncr } from '../api/ncrs.ts'
import CompleteActionButton from './CompleteActionButton.tsx'
import DateTime from './DateTime.tsx'
import { isAssignedInvestigator } from './permissions.ts'

type CorrectiveActionsProps = {
  ncr: Ncr
  currentUser: CurrentUser
}

/** The NCR's corrective actions; the assigned investigator can complete each one while actions are in progress. */
function CorrectiveActions({ ncr, currentUser }: CorrectiveActionsProps) {
  const canComplete = ncr.status === 'ActionInProgress' && isAssignedInvestigator(ncr, currentUser)

  return (
    <section aria-labelledby="ncr-actions">
      <h2 id="ncr-actions" className="text-lg font-semibold">
        Corrective actions
      </h2>
      {ncr.actions.length === 0 ? (
        <p className="mt-2 text-slate-700">No corrective actions yet.</p>
      ) : (
        <ul className="mt-2 space-y-2">
          {ncr.actions.map((action) => (
            <li key={action.id} className="flex flex-wrap items-center gap-x-3 gap-y-1">
              <span>{action.description}</span>
              <span className="text-sm text-slate-600">
                {action.completedAt ? (
                  <>
                    (completed <DateTime iso={action.completedAt} />)
                  </>
                ) : (
                  '(not completed)'
                )}
              </span>
              {canComplete && !action.completedAt && (
                <CompleteActionButton ncrId={ncr.id} actionId={action.id} description={action.description} />
              )}
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}

export default CorrectiveActions
