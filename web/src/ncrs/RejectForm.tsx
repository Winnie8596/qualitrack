import { zodResolver } from '@hookform/resolvers/zod'
import { useId } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { toFormErrors } from '../api/formErrors.ts'
import { rejectVerification } from '../api/ncrs.ts'
import FormAlert from '../forms/FormAlert.tsx'
import TextAreaField from '../forms/TextAreaField.tsx'
import { dangerButtonClass } from '../forms/fieldClass.ts'
import { useNcrCommand } from './useNcrCommand.ts'

// BR-7: rejecting needs a reason, which the investigator reads in the history
const rejectSchema = z.object({ reason: z.string().trim().min(1, 'Enter a reason.') })

type RejectFormProps = {
  ncrId: string
}

/** C2: the fix goes back to the investigator, with actions in progress again. */
function RejectForm({ ncrId }: RejectFormProps) {
  const headingId = useId()
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<z.input<typeof rejectSchema>, unknown, z.output<typeof rejectSchema>>({
    resolver: zodResolver(rejectSchema),
    defaultValues: { reason: '' },
  })
  const reject = useNcrCommand(({ reason }: z.output<typeof rejectSchema>) => rejectVerification(ncrId, reason), {
    onError: (error) => {
      const message = toFormErrors(error, ['reason']).fields.reason
      if (message) {
        setError('reason', { type: 'server', message })
      }
    },
  })

  const formError = reject.error ? toFormErrors(reject.error, ['reason']).form : undefined

  return (
    <form
      aria-labelledby={headingId}
      noValidate
      onSubmit={handleSubmit((request) => reject.mutate(request))}
      className="space-y-4"
    >
      <h3 id={headingId} className="font-semibold">
        Reject the fix
      </h3>
      {formError && <FormAlert message={formError} />}
      <TextAreaField label="Reason" registration={register('reason')} error={errors.reason?.message} />
      <button type="submit" disabled={reject.isPending} className={dangerButtonClass}>
        {reject.isPending ? 'Rejecting…' : 'Reject'}
      </button>
    </form>
  )
}

export default RejectForm
