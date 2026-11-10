import type { Role } from '../api/auth.ts'

export const roleLabels: Record<Role, string> = {
  Inspector: 'Inspector',
  Investigator: 'Investigator',
  QaManager: 'QA Manager',
}
