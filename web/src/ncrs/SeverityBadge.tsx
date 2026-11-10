import type { Severity } from '../api/ncrs.ts'

const severityClasses: Record<Severity, string> = {
  Minor: 'bg-slate-100 text-slate-800 ring-slate-300',
  Major: 'bg-orange-100 text-orange-900 ring-orange-300',
  Critical: 'bg-red-100 text-red-900 ring-red-300',
}

type SeverityBadgeProps = {
  severity: Severity
}

function SeverityBadge({ severity }: SeverityBadgeProps) {
  return (
    <span className={`inline-block rounded-full px-2.5 py-0.5 text-sm font-medium ring-1 ${severityClasses[severity]}`}>
      {severity}
    </span>
  )
}

export default SeverityBadge
