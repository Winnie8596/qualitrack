import type { NcrStatus } from '../api/ncrs.ts'

export const statusLabels: Record<NcrStatus, string> = {
  Open: 'Open',
  Investigating: 'Investigating',
  ActionInProgress: 'Action in progress',
  PendingVerification: 'Pending verification',
  Closed: 'Closed',
  Cancelled: 'Cancelled',
}

// Times as the reader's browser writes them, in their own time zone
const dateTimeFormat = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' })

export function formatDateTime(iso: string): string {
  return dateTimeFormat.format(new Date(iso))
}
