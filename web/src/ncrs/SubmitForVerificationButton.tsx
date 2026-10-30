import { toFormErrors } from '../api/formErrors.ts'
import { submitForVerification } from '../api/ncrs.ts'
import { primaryButtonClass } from '../forms/fieldClass.ts'
import FormAlert from '../forms/FormAlert.tsx'
import { useNcrCommand } from './useNcrCommand.ts'

type SubmitForVerificationButtonProps = {
  ncrId: string
}

/** B3. Always offered: when BR-4 refuses it, the API says why, which explains more than a greyed-out button. */
function SubmitForVerificationButton({ ncrId }: SubmitForVerificationButtonProps) {
  const submit = useNcrCommand(() => submitForVerification(ncrId))

  return (
    <div className="space-y-3">
      {submit.error && <FormAlert message={toFormErrors(submit.error, []).form ?? 'Not submitted.'} />}
      <button
        type="button"
        disabled={submit.isPending}
        onClick={() => submit.mutate(undefined)}
        className={primaryButtonClass}
      >
        {submit.isPending ? 'Submitting…' : 'Submit for verification'}
      </button>
    </div>
  )
}

export default SubmitForVerificationButton
