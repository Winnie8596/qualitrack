import { completeCorrectiveAction } from '../api/ncrs.ts'
import { toFormErrors } from '../api/formErrors.ts'
import FormAlert from '../forms/FormAlert.tsx'
import { useNcrCommand } from './useNcrCommand.ts'

type CompleteActionButtonProps = {
  ncrId: string
  actionId: string
  description: string
}

const secondaryButtonClass =
  'rounded border border-blue-700 px-3 py-1 text-sm font-medium text-blue-800 hover:bg-blue-50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-700 disabled:opacity-60'

/** B2. Named after its action, so a screen reader user hears which one each button completes. */
function CompleteActionButton({ ncrId, actionId, description }: CompleteActionButtonProps) {
  const complete = useNcrCommand(() => completeCorrectiveAction(ncrId, actionId))

  return (
    <>
      <button
        type="button"
        aria-label={`Mark "${description}" complete`}
        disabled={complete.isPending}
        onClick={() => complete.mutate(undefined)}
        className={secondaryButtonClass}
      >
        {complete.isPending ? 'Completing…' : 'Mark complete'}
      </button>
      {complete.error && <FormAlert message={toFormErrors(complete.error, []).form ?? 'Not completed.'} />}
    </>
  )
}

export default CompleteActionButton
