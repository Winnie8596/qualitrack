import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { NcrStatus } from '../api/ncrs.ts'
import StatusBadge from './StatusBadge.tsx'

describe('StatusBadge', () => {
  it.each<[NcrStatus, string]>([
    ['Open', 'Open'],
    ['Investigating', 'Investigating'],
    ['ActionInProgress', 'Action in progress'],
    ['PendingVerification', 'Pending verification'],
    ['Closed', 'Closed'],
    ['Cancelled', 'Cancelled'],
  ])('names the %s status in words, so colour is not the only signal', (status, words) => {
    render(<StatusBadge status={status} />)

    expect(screen.getByText(words)).toBeInTheDocument()
  })

  it('gives each status its own colour', () => {
    const statuses: NcrStatus[] = ['Open', 'Investigating', 'ActionInProgress', 'PendingVerification', 'Closed', 'Cancelled']

    const classNames = statuses.map((status) => {
      const { container } = render(<StatusBadge status={status} />)
      return (container.firstElementChild as HTMLElement).className
    })

    expect(new Set(classNames).size).toBe(statuses.length)
  })
})
