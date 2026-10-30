import { formatDateTime } from './labels.ts'

type DateTimeProps = {
  iso: string
}

// <time> keeps the exact instant machine-readable while showing it in the reader's own format
function DateTime({ iso }: DateTimeProps) {
  return <time dateTime={iso}>{formatDateTime(iso)}</time>
}

export default DateTime
