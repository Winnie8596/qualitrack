import { toFormErrors } from '../api/formErrors.ts'
import { verifyNcr, type Ncr } from '../api/ncrs.ts'
import { primaryButtonClass } from '../forms/fieldClass.ts'
import FormAlert from '../forms/FormAlert.tsx'
import RejectForm from './RejectForm.tsx'
import { useNcrCommand } from './useNcrCommand.ts'

type VerificationControlsProps = {
  ncr: Ncr
}

/**
 * C1 and C2, for a QA manager once the fix is submitted. The caller decides who sees this; this decides when.
 * The API also refuses a verifier who is the investigator (BR-5), which one role per user already rules out.
 */
function VerificationControls({ ncr }: VerificationControlsProps) {
  const verify = useNcrCommand(() => verifyNcr(ncr.id))

  if (ncr.status !== 'PendingVerification') {
    return null
  }

  return (
    <section aria-labelledby="ncr-verification" className="space-y-6">
      <h2 id="ncr-verification" className="text-lg font-semibold">
        Verification
      </h2>
      <div className="grid gap-8 md:grid-cols-2">
        <div className="space-y-3">
          <p className="text-slate-700">Check the corrective actions and root cause above before closing.</p>
          {verify.error && <FormAlert message={toFormErrors(verify.error, []).form ?? 'Not verified.'} />}
          <button
            type="button"
            disabled={verify.isPending}
            onClick={() => verify.mutate(undefined)}
            className={primaryButtonClass}
          >
            {verify.isPending ? 'Verifying…' : 'Verify and close'}
          </button>
        </div>
        <RejectForm ncrId={ncr.id} />
      </div>
    </section>
  )
}

export default VerificationControls
