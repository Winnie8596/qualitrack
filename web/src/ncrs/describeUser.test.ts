import { expect, test } from 'vitest'
import { describeUser } from './describeUser.ts'

// Real ids of two demo users created in the same second: time-ordered (version 7), so they start alike
const inspector = '01a1c471-99fd-77dc-aa85-95fd31e1584e'
const investigator = '01a1c471-9b7c-7648-ba55-5797a34ee9ea'

test('the reader is "You"', () => {
  expect(describeUser(inspector, inspector)).toBe('You')
})

test('two users created at the same moment are told apart', () => {
  const reader = '0199aaaa-0000-7000-8000-000000000000'

  expect(describeUser(inspector, reader)).not.toBe(describeUser(investigator, reader))
})
