import { useId } from 'react'
import type { UseFormRegisterReturn } from 'react-hook-form'
import { fieldClass } from './fieldClass.ts'

type SelectFieldProps = {
  label: string
  registration: UseFormRegisterReturn
  options: readonly { value: string; label: string }[]
  // Shown first with the value "", so nothing is chosen until the user chooses
  placeholder: string
  error?: string
}

// The error is tied to the select with aria-describedby, so a screen reader reads it with the label.
function SelectField({ label, registration, options, placeholder, error }: SelectFieldProps) {
  const id = useId()
  const errorId = `${id}-error`

  return (
    <div>
      <label htmlFor={id} className="block font-medium">
        {label}
      </label>
      <select
        id={id}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? errorId : undefined}
        className={fieldClass}
        {...registration}
      >
        <option value="">{placeholder}</option>
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
      {error && (
        <p id={errorId} className="mt-1 text-sm text-red-700">
          {error}
        </p>
      )}
    </div>
  )
}

export default SelectField
