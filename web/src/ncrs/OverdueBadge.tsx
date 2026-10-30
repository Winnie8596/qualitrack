// Written out, so overdue is never shown by colour alone (PRD §8)
function OverdueBadge() {
  return (
    <span className="inline-block rounded-full bg-red-100 px-2.5 py-0.5 text-sm font-medium text-red-900 ring-1 ring-red-300">
      Overdue
    </span>
  )
}

export default OverdueBadge
