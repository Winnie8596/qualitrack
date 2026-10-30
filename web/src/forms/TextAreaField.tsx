import { useId } from 'react'
import type { UseFormRegisterReturn } from 'react-hook-form'
import { fieldClass } from './fieldClass.ts'

type TextAreaFieldProps = {
  label: string
  registration: UseFormRegisterReturn
  error?: string
}

function TextAreaField({ label, registration, error }: TextAreaFieldProps) {
  const id = useId()
  const errorId = `${id}-error`

  return (
    <div>
      <label htmlFor={id} className="block font-medium">
        {label}
      </label>
      <textarea
        id={id}
        rows={5}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? errorId : undefined}
        className={fieldClass}
        {...registration}
      />
      {error && (
        <p id={errorId} className="mt-1 text-sm text-red-700">
          {error}
        </p>
      )}
    </div>
  )
}

export default TextAreaField
