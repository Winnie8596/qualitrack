import type { NcrStatus } from '../api/ncrs.ts'
import { statusLabels } from './labels.ts'

// Whole class names, one per status: Tailwind only generates classes it finds written out in the source (ADR 0007).
const statusClasses: Record<NcrStatus, string> = {
  Open: 'bg-sky-100 text-sky-900 ring-sky-300',
  Investigating: 'bg-violet-100 text-violet-900 ring-violet-300',
  ActionInProgress: 'bg-amber-100 text-amber-900 ring-amber-300',
  PendingVerification: 'bg-teal-100 text-teal-900 ring-teal-300',
  Closed: 'bg-green-100 text-green-900 ring-green-300',
  Cancelled: 'bg-slate-200 text-slate-800 ring-slate-400',
}

type StatusBadgeProps = {
  status: NcrStatus
}

// The status is written out, so colour is never the only signal (PRD §8).
function StatusBadge({ status }: StatusBadgeProps) {
  return (
    <span className={`inline-block rounded-full px-2.5 py-0.5 text-sm font-medium ring-1 ${statusClasses[status]}`}>
      {statusLabels[status]}
    </span>
  )
}

export default StatusBadge
