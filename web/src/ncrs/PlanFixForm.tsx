import { zodResolver } from '@hookform/resolvers/zod'
import { useId } from 'react'
import { useFieldArray, useForm } from 'react-hook-form'
import { z } from 'zod'
import { toFormErrors } from '../api/formErrors.ts'
import { planFix } from '../api/ncrs.ts'
import { primaryButtonClass } from '../forms/fieldClass.ts'
import FormAlert from '../forms/FormAlert.tsx'
import TextAreaField from '../forms/TextAreaField.tsx'
import TextField from '../forms/TextField.tsx'
import { useNcrCommand } from './useNcrCommand.ts'

// B1: a root cause and at least one action, each described; the domain checks the same again
const planFixSchema = z.object({
  rootCause: z.string().trim().min(1, 'Enter the root cause.'),
  actions: z.array(z.object({ description: z.string().trim().min(1, 'Describe the action.') })).min(1),
})

type PlanFixValues = z.input<typeof planFixSchema>

const linkButtonClass = 'font-medium text-blue-800 underline underline-offset-4 hover:text-blue-900'

type PlanFixFormProps = {
  ncrId: string
}

/** B1, for the assigned investigator while the NCR is being investigated. */
function PlanFixForm({ ncrId }: PlanFixFormProps) {
  const headingId = useId()
  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<PlanFixValues, unknown, z.output<typeof planFixSchema>>({
    resolver: zodResolver(planFixSchema),
    defaultValues: { rootCause: '', actions: [{ description: '' }] },
  })
  // React Hook Form keys each row, so removing one keeps what was typed in the others
  const actions = useFieldArray({ control, name: 'actions' })
  const plan = useNcrCommand(
    ({ rootCause, actions: planned }: z.output<typeof planFixSchema>) =>
      planFix(ncrId, { rootCause, actions: planned.map((action) => action.description) }),
    {
      onError: (error) => {
        const message = toFormErrors(error, ['rootCause']).fields.rootCause
        if (message) {
          setError('rootCause', { type: 'server', message })
        }
      },
    },
  )

  const formError = plan.error ? toFormErrors(plan.error, ['rootCause']).form : undefined

  return (
    <form
      aria-labelledby={headingId}
      noValidate
      onSubmit={handleSubmit((request) => plan.mutate(request))}
      className="space-y-4"
    >
      <h3 id={headingId} className="font-semibold">
        Plan the fix
      </h3>
      {formError && <FormAlert message={formError} />}
      <TextAreaField label="Root cause" registration={register('rootCause')} error={errors.rootCause?.message} />

      <fieldset className="space-y-3">
        <legend className="font-medium">Corrective actions</legend>
        {actions.fields.map((field, index) => (
          <div key={field.id} className="flex items-end gap-3">
            <div className="flex-1">
              <TextField
                label={`Corrective action ${index + 1}`}
                registration={register(`actions.${index}.description`)}
                error={errors.actions?.[index]?.description?.message}
              />
            </div>
            {actions.fields.length > 1 && (
              <button
                type="button"
                aria-label={`Remove corrective action ${index + 1}`}
                onClick={() => actions.remove(index)}
                className={`${linkButtonClass} py-2`}
              >
                Remove
              </button>
            )}
          </div>
        ))}
        <button type="button" onClick={() => actions.append({ description: '' })} className={linkButtonClass}>
          Add another action
        </button>
      </fieldset>

      <button type="submit" disabled={plan.isPending} className={primaryButtonClass}>
        {plan.isPending ? 'Saving…' : 'Plan the fix'}
      </button>
    </form>
  )
}

export default PlanFixForm
