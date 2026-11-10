import { ApiError } from './client.ts'

export type FormErrors<Field extends string> = {
  // For the alert above the form: what went wrong that no single field explains
  form?: string
  fields: Partial<Record<Field, string>>
}

const unreachable = 'QualiTrack cannot be reached. Check your connection and try again.'

/**
 * Turns a failed request into messages a form can show: validation errors on the fields the form has, everything
 * else (a domain rule, a role, an error for a field the form lacks) as one message for the whole form.
 */
export function toFormErrors<Field extends string>(error: unknown, fieldNames: readonly Field[]): FormErrors<Field> {
  if (!(error instanceof ApiError)) {
    return { form: unreachable, fields: {} }
  }

  const fields: Partial<Record<Field, string>> = {}
  const otherMessages: string[] = []
  for (const [name, messages] of Object.entries(error.problem.errors ?? {})) {
    const field = fieldNames.find((known) => known === name)
    if (field) {
      fields[field] = messages.join(' ')
    } else {
      otherMessages.push(...messages)
    }
  }

  const hasFieldErrors = Object.keys(fields).length > 0
  const form =
    otherMessages.length > 0
      ? otherMessages.join(' ')
      : hasFieldErrors
        ? undefined
        : (error.problem.detail ?? error.problem.title ?? error.message)

  return form === undefined ? { fields } : { form, fields }
}
