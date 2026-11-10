import { expect, test, vi } from 'vitest'
import { z } from './zod.ts'

test('schemas are built and used without compiling code at runtime, which the Content-Security-Policy forbids', () => {
  const compile = vi.spyOn(globalThis, 'Function')

  const parsed = z.object({ title: z.string() }).parse({ title: 'Burr on flange' })

  expect(parsed).toEqual({ title: 'Burr on flange' })
  expect(compile).not.toHaveBeenCalled()
})
