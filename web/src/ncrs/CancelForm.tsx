import { zodResolver } from '@hookform/resolvers/zod'
import { useId } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { toFormErrors } from '../api/formErrors.ts'
import { cancelNcr } from '../api/ncrs.ts'
import { dangerButtonClass } from '../forms/fieldClass.ts'
import FormAlert from '../forms/FormAlert.tsx'
import TextAreaField from '../forms/TextAreaField.tsx'
import { useNcrCommand } from './useNcrCommand.ts'

// BR-7: cancelling needs a reason, which goes into the history
const cancelSchema = z.object({ reason: z.string().trim().min(1, 'Enter a reason.') })

const cancelFields = ['reason'] as const

type CancelFormProps = {
  ncrId: string
}

/** A3, for a QA manager on an NCR that is not finished. */
function CancelForm({ ncrId }: CancelFormProps) {
  const headingId = useId()
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<z.input<typeof cancelSchema>, unknown, z.output<typeof cancelSchema>>({
    resolver: zodResolver(cancelSchema),
    defaultValues: { reason: '' },
  })
  const cancel = useNcrCommand(({ reason }: z.output<typeof cancelSchema>) => cancelNcr(ncrId, reason), {
    onError: (error) => {
      const message = toFormErrors(error, cancelFields).fields.reason
      if (message) {
        setError('reason', { type: 'server', message })
      }
    },
  })

  const formError = cancel.error ? toFormErrors(cancel.error, cancelFields).form : undefined

  return (
    <form
      aria-labelledby={headingId}
      noValidate
      onSubmit={handleSubmit((request) => cancel.mutate(request))}
      className="space-y-4"
    >
      <h3 id={headingId} className="font-semibold">
        Cancel this NCR
      </h3>
      {formError && <FormAlert message={formError} />}
      <TextAreaField label="Reason" registration={register('reason')} error={errors.reason?.message} />
      <button type="submit" disabled={cancel.isPending} className={dangerButtonClass}>
        {cancel.isPending ? 'Cancelling…' : 'Cancel NCR'}
      </button>
    </form>
  )
}

export default CancelForm
