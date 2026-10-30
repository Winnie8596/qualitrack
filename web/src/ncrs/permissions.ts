import type { CurrentUser } from '../api/auth.ts'
import type { Ncr } from '../api/ncrs.ts'

// Which controls a user is shown (PRD §4). The API enforces the same rules; these only hide what it would refuse.

// Triage (A2, A3) and verification (C1, C2)
export function isQaManager(user: CurrentUser): boolean {
  return user.role === 'QaManager'
}

// BR-3: only the assigned investigator works on the fix
export function isAssignedInvestigator(ncr: Ncr, user: CurrentUser): boolean {
  return user.role === 'Investigator' && ncr.investigatorId === user.id
}
