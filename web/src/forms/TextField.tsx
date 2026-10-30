import { useId, type InputHTMLAttributes } from 'react'
import type { UseFormRegisterReturn } from 'react-hook-form'
import { fieldClass } from './fieldClass.ts'

type TextFieldProps = {
  label: string
  registration: UseFormRegisterReturn
  error?: string
  type?: 'text' | 'email' | 'password' | 'datetime-local'
  autoComplete?: InputHTMLAttributes<HTMLInputElement>['autoComplete']
}

// The error is tied to the input with aria-describedby, so a screen reader reads it with the label.
function TextField({ label, registration, error, type = 'text', autoComplete }: TextFieldProps) {
  const id = useId()
  const errorId = `${id}-error`

  return (
    <div>
      <label htmlFor={id} className="block font-medium">
        {label}
      </label>
      <input
        id={id}
        type={type}
        autoComplete={autoComplete}
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

export default TextField
