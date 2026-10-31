import { useId } from 'react'
import type { UseFormRegisterReturn } from 'react-hook-form'

type FileFieldProps = {
  label: string
  registration: UseFormRegisterReturn
  // What the picker offers, e.g. "image/jpeg,image/png"; a hint only, so the form still checks the file
  accept: string
  hint: string
  error?: string
}

// The hint and any error are tied to the input with aria-describedby, so a screen reader reads them with the label.
function FileField({ label, registration, accept, hint, error }: FileFieldProps) {
  const id = useId()
  const hintId = `${id}-hint`
  const errorId = `${id}-error`

  return (
    <div>
      <label htmlFor={id} className="block font-medium">
        {label}
      </label>
      <input
        id={id}
        type="file"
        accept={accept}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? errorId : hintId}
        className="mt-1 block w-full text-sm file:mr-4 file:rounded file:border-0 file:bg-blue-50 file:px-4 file:py-2 file:font-medium file:text-blue-800 hover:file:bg-blue-100"
        {...registration}
      />
      {error ? (
        <p id={errorId} className="mt-1 text-sm text-red-700">
          {error}
        </p>
      ) : (
        <p id={hintId} className="mt-1 text-sm text-slate-600">
          {hint}
        </p>
      )}
    </div>
  )
}

export default FileField
