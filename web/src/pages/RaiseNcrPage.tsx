import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import { useId } from 'react'
import { useForm } from 'react-hook-form'
import { useNavigate } from 'react-router'
import { z } from 'zod'
import { toFormErrors } from '../api/formErrors.ts'
import { raiseNcr, severitySchema } from '../api/ncrs.ts'
import { primaryButtonClass } from '../forms/fieldClass.ts'
import FormAlert from '../forms/FormAlert.tsx'
import TextAreaField from '../forms/TextAreaField.tsx'
import TextField from '../forms/TextField.tsx'

// BR-1, checked here too so the user hears at once; the API and the domain check it again
const raiseSchema = z.object({
  title: z.string().trim().min(1, 'Enter a title.'),
  description: z.string().trim().min(1, 'Enter a description.'),
  location: z.string().trim().min(1, 'Enter a location.'),
  severity: z.enum(severitySchema.options, { error: 'Choose a severity.' }),
})

type RaiseForm = z.input<typeof raiseSchema>

const raiseFields = ['title', 'description', 'location', 'severity'] as const

function RaiseNcrPage() {
  const navigate = useNavigate()
  const severityId = useId()
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<RaiseForm, unknown, z.output<typeof raiseSchema>>({
    resolver: zodResolver(raiseSchema),
    defaultValues: { title: '', description: '', location: '' },
  })
  const raise = useMutation({
    mutationFn: raiseNcr,
    onSuccess: (id) => navigate(`/ncrs/${id}`),
    onError: (error) => {
      const { fields } = toFormErrors(error, raiseFields)
      for (const field of raiseFields) {
        const message = fields[field]
        if (message) {
          setError(field, { type: 'server', message })
        }
      }
    },
  })

  const formError = raise.error ? toFormErrors(raise.error, raiseFields).form : undefined
  const severityError = errors.severity?.message

  return (
    <div className="max-w-xl">
      <h1 className="text-2xl font-semibold">Raise an NCR</h1>
      <form noValidate onSubmit={handleSubmit((request) => raise.mutate(request))} className="mt-6 space-y-4">
        {formError && <FormAlert message={formError} />}
        <TextField label="Title" registration={register('title')} error={errors.title?.message} />
        <TextAreaField label="Description" registration={register('description')} error={errors.description?.message} />
        <TextField label="Location" registration={register('location')} error={errors.location?.message} />

        <fieldset
          role="radiogroup"
          aria-labelledby={`${severityId}-legend`}
          aria-invalid={severityError ? true : undefined}
          aria-describedby={severityError ? `${severityId}-error` : undefined}
        >
          <legend id={`${severityId}-legend`} className="font-medium">
            Severity
          </legend>
          <div className="mt-1 flex flex-wrap gap-x-6 gap-y-2">
            {severitySchema.options.map((severity) => (
              <label key={severity} className="flex items-center gap-2">
                <input type="radio" value={severity} className="size-4 accent-blue-700" {...register('severity')} />
                {severity}
              </label>
            ))}
          </div>
          {severityError && (
            <p id={`${severityId}-error`} className="mt-1 text-sm text-red-700">
              {severityError}
            </p>
          )}
        </fieldset>

        <button type="submit" disabled={raise.isPending} className={primaryButtonClass}>
          {raise.isPending ? 'Raising…' : 'Raise NCR'}
        </button>
      </form>
    </div>
  )
}

export default RaiseNcrPage
