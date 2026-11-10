import { describe, expect, it } from 'vitest'
import { ApiError } from './client.ts'
import { toFormErrors } from './formErrors.ts'

const raiseFields = ['title', 'description', 'location', 'severity'] as const

describe('toFormErrors', () => {
  it('puts each validation error on the field the form sent', () => {
    const error = new ApiError(400, {
      title: 'One or more validation errors occurred.',
      errors: { title: ['The Title field is required.'], severity: ['The Severity field is required.'] },
    })

    const result = toFormErrors(error, raiseFields)

    expect(result).toEqual({
      fields: { title: 'The Title field is required.', severity: 'The Severity field is required.' },
    })
  })

  it('joins several messages for one field', () => {
    const error = new ApiError(400, { errors: { title: ['Too short.', 'Has invalid characters.'] } })

    const result = toFormErrors(error, raiseFields)

    expect(result.fields.title).toBe('Too short. Has invalid characters.')
  })

  it('shows errors for fields the form does not have as a form error', () => {
    const error = new ApiError(400, {
      title: 'One or more validation errors occurred.',
      errors: { credentials: ['Email and password are required.'] },
    })

    const result = toFormErrors(error, raiseFields)

    expect(result).toEqual({ form: 'Email and password are required.', fields: {} })
  })

  it('shows the detail of a problem with no field errors as a form error', () => {
    const error = new ApiError(409, { title: 'Conflict', detail: 'A closed NCR cannot be cancelled.' })

    const result = toFormErrors(error, raiseFields)

    expect(result).toEqual({ form: 'A closed NCR cannot be cancelled.', fields: {} })
  })

  it('falls back to the title when there is no detail', () => {
    const error = new ApiError(401, { title: 'Email or password is wrong.' })

    const result = toFormErrors(error, raiseFields)

    expect(result.form).toBe('Email or password is wrong.')
  })

  it('says the service cannot be reached when the request never got an answer', () => {
    const result = toFormErrors(new TypeError('Failed to fetch'), raiseFields)

    expect(result).toEqual({ form: 'QualiTrack cannot be reached. Check your connection and try again.', fields: {} })
  })
})
