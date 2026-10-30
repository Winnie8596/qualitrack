import { Link } from 'react-router'

// Also shown for an NCR the user may not see: the API answers 404 for both, so ids cannot be probed.
function NcrNotFound() {
  return (
    <>
      <h1 className="text-2xl font-semibold">NCR not found</h1>
      <p className="mt-2 text-slate-700">
        This NCR does not exist, or you do not have access to it.{' '}
        <Link to="/" className="text-blue-800 underline underline-offset-4">
          Go to the home page
        </Link>
      </p>
    </>
  )
}

export default NcrNotFound
