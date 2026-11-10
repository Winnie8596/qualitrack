// One look for every text input; aria-invalid turns the border red alongside the written error.
export const fieldClass =
  'mt-1 block w-full rounded border border-slate-400 bg-white px-3 py-2 focus:border-blue-700 focus:outline-2 focus:outline-blue-700 aria-invalid:border-red-700'

// Red, for what cannot be undone (BR-6: a cancelled NCR stays cancelled)
export const dangerButtonClass =
  'rounded bg-red-700 px-4 py-2 font-medium text-white hover:bg-red-800 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-red-700 disabled:opacity-60'

export const primaryButtonClass =
  'rounded bg-blue-700 px-4 py-2 font-medium text-white hover:bg-blue-800 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-700 disabled:opacity-60'
