import type { Ncr } from '../api/ncrs.ts'
import { userIds } from './fakeApi.ts'

/** An NCR as GET /api/ncrs/{id} returns it: open, just raised by the inspector. Override what a test cares about. */
export function anNcr(overrides: Partial<Ncr> = {}): Ncr {
  return {
    id: '0199a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a5b',
    number: 'NCR-2026-0001',
    title: 'Weld crack on bracket',
    description: 'Hairline crack along the fillet weld.',
    location: 'Bay 3',
    severity: 'Major',
    status: 'Open',
    raisedBy: userIds.inspector,
    raisedAt: '2026-10-30T08:15:00.1234567+00:00',
    investigatorId: null,
    dueDate: null,
    isOverdue: false,
    rootCause: null,
    actions: [],
    history: [],
    ...overrides,
  }
}
