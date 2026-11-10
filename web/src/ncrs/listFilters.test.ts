import { describe, expect, it } from 'vitest'
import { defaultListFilters, readListFilters, toSearchParams } from './listFilters.ts'

describe('readListFilters', () => {
  it('reads every filter from the address', () => {
    const filters = readListFilters(
      new URLSearchParams(
        'status=Investigating&severity=Critical&overdueOnly=true&sortBy=DueDate&direction=Ascending&page=3',
      ),
    )

    expect(filters).toEqual({
      status: 'Investigating',
      severity: 'Critical',
      overdueOnly: true,
      sortBy: 'DueDate',
      direction: 'Ascending',
      page: 3,
    })
  })

  it('uses the defaults for an empty address', () => {
    expect(readListFilters(new URLSearchParams())).toEqual(defaultListFilters)
  })

  it('ignores values it does not know, so an edited address still shows a list', () => {
    const filters = readListFilters(new URLSearchParams('status=Lost&severity=1&page=0&sortBy=Title&overdueOnly=yes'))

    expect(filters).toEqual(defaultListFilters)
  })
})

describe('toSearchParams', () => {
  it('leaves out the defaults, so the plain list has a plain address', () => {
    expect(toSearchParams(defaultListFilters).toString()).toBe('')
  })

  it('writes what differs from the defaults in a fixed order', () => {
    const params = toSearchParams({ ...defaultListFilters, page: 2, severity: 'Minor', overdueOnly: true })

    expect(params.toString()).toBe('severity=Minor&overdueOnly=true&page=2')
  })
})
