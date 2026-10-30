import { zodResolver } from '@hookform/resolvers/zod'
import { useQuery } from '@tanstack/react-query'
import { useId } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { toFormErrors } from '../api/formErrors.ts'
import { assignInvestigator } from '../api/ncrs.ts'
import { investigatorsQueryOptions } from '../api/users.ts'
import { primaryButtonClass } from '../forms/fieldClass.ts'
import FormAlert from '../forms/FormAlert.tsx'
import SelectField from '../forms/SelectField.tsx'
import TextField from '../forms/TextField.tsx'
import { useNcrCommand } from './useNcrCommand.ts'

// Only that both are there; BR-2 (future, and within 7 days for a critical NCR) is the domain's to check
const assignSchema = z.object({
  investigatorId: z.string().min(1, 'Choose an investigator.'),
  // A datetime-local value is the reader's own local time, which Date reads it as
  dueDate: z
    .string()
    .min(1, 'Choose a due date.')
    .transform((value) => new Date(value).toISOString()),
})

const assignFields = ['investigatorId', 'dueDate'] as const

type AssignFormProps = {
  ncrId: string
}

/** A2, for a QA manager on an open NCR. */
function AssignForm({ ncrId }: AssignFormProps) {
  const headingId = useId()
  const investigators = useQuery(investigatorsQueryOptions)
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<z.input<typeof assignSchema>, unknown, z.output<typeof assignSchema>>({
    resolver: zodResolver(assignSchema),
    defaultValues: { investigatorId: '', dueDate: '' },
  })
  const assign = useNcrCommand((request: z.output<typeof assignSchema>) => assignInvestigator(ncrId, request), {
    onError: (error) => {
      const { fields } = toFormErrors(error, assignFields)
      for (const field of assignFields) {
        const message = fields[field]
        if (message) {
          setError(field, { type: 'server', message })
        }
      }
    },
  })

  const formError = assign.error ? toFormErrors(assign.error, assignFields).form : undefined
  const options = (investigators.data ?? []).map((user) => ({ value: user.id, label: user.email }))

  return (
    <form
      aria-labelledby={headingId}
      noValidate
      onSubmit={handleSubmit((request) => assign.mutate(request))}
      className="space-y-4"
    >
      <h3 id={headingId} className="font-semibold">
        Assign an investigator
      </h3>
      {formError && <FormAlert message={formError} />}
      {investigators.error && <FormAlert message="The investigators could not be loaded. Try again in a few minutes." />}
      <SelectField
        label="Investigator"
        registration={register('investigatorId')}
        options={options}
        placeholder={investigators.isPending ? 'Loading investigators…' : 'Choose an investigator'}
        error={errors.investigatorId?.message}
      />
      <TextField label="Due" type="datetime-local" registration={register('dueDate')} error={errors.dueDate?.message} />
      <button type="submit" disabled={assign.isPending} className={primaryButtonClass}>
        {assign.isPending ? 'Assigning…' : 'Assign'}
      </button>
    </form>
  )
}

export default AssignForm
