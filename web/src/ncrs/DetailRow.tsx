import type { ReactNode } from 'react'

type DetailRowProps = {
  term: string
  children: ReactNode
}

// One term and its value in a <dl>: stacked on a phone, side by side from the sm breakpoint
function DetailRow({ term, children }: DetailRowProps) {
  return (
    <div className="py-2 sm:grid sm:grid-cols-3 sm:gap-4">
      <dt className="font-medium text-slate-600">{term}</dt>
      <dd className="mt-1 sm:col-span-2 sm:mt-0">{children}</dd>
    </div>
  )
}

export default DetailRow
