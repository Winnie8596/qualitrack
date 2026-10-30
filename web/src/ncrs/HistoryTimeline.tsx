import type { Ncr } from '../api/ncrs.ts'
import DateTime from './DateTime.tsx'
import { describeUser } from './describeUser.ts'
import { statusLabels } from './labels.ts'

type HistoryTimelineProps = {
  history: Ncr['history']
  currentUserId: string
}

/** Every status change in order, oldest first, with who made it, when, and any note (BR-7). */
function HistoryTimeline({ history, currentUserId }: HistoryTimelineProps) {
  return (
    <section aria-labelledby="ncr-history">
      <h2 id="ncr-history" className="text-lg font-semibold">
        History
      </h2>
      {history.length === 0 ? (
        <p className="mt-2 text-slate-700">No status changes yet.</p>
      ) : (
        // An ordered list, because the order is the point; the line and dots only draw it
        <ol className="mt-3 space-y-4 border-l-2 border-slate-300 pl-5">
          {history.map((change) => (
            <li
              key={`${change.at}-${change.to}`}
              className="relative before:absolute before:top-1.5 before:-left-[1.6rem] before:size-3 before:rounded-full before:bg-blue-700"
            >
              <span className="font-medium">
                {statusLabels[change.from]} → {statusLabels[change.to]}
              </span>{' '}
              <span className="text-sm text-slate-600">
                by {describeUser(change.by, currentUserId)}, <DateTime iso={change.at} />
              </span>
              {change.note && <p className="text-slate-700">{change.note}</p>}
            </li>
          ))}
        </ol>
      )}
    </section>
  )
}

export default HistoryTimeline
