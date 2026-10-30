import { zodResolver } from '@hookform/resolvers/zod'
import { useId } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { toFormErrors } from '../api/formErrors.ts'
import { addCorrectiveAction } from '../api/ncrs.ts'
import { primaryButtonClass } from '../forms/fieldClass.ts'
import FormAlert from '../forms/FormAlert.tsx'
import TextField from '../forms/TextField.tsx'
import { useNcrCommand } from './useNcrCommand.ts'

const addActionSchema = z.object({ description: z.string().trim().min(1, 'Describe the action.') })

type AddActionFormProps = {
  ncrId: string
}

/** B2: one more corrective action, e.g. after a rejected verification. */
function AddActionForm({ ncrId }: AddActionFormProps) {
  const headingId = useId()
  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors },
  } = useForm<z.input<typeof addActionSchema>, unknown, z.output<typeof addActionSchema>>({
    resolver: zodResolver(addActionSchema),
    defaultValues: { description: '' },
  })
  const add = useNcrCommand(
    ({ description }: z.output<typeof addActionSchema>) => addCorrectiveAction(ncrId, description),
    {
      onError: (error) => {
        const message = toFormErrors(error, ['description']).fields.description
        if (message) {
          setError('description', { type: 'server', message })
        }
      },
    },
  )

  const formError = add.error ? toFormErrors(add.error, ['description']).form : undefined

  return (
    <form
      aria-labelledby={headingId}
      noValidate
      onSubmit={handleSubmit((request) => add.mutate(request, { onSuccess: () => reset() }))}
      className="space-y-4"
    >
      <h3 id={headingId} className="font-semibold">
        Add a corrective action
      </h3>
      {formError && <FormAlert message={formError} />}
      <TextField label="Description" registration={register('description')} error={errors.description?.message} />
      <button type="submit" disabled={add.isPending} className={primaryButtonClass}>
        {add.isPending ? 'Adding…' : 'Add action'}
      </button>
    </form>
  )
}

export default AddActionForm
