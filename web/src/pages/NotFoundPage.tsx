import { Link } from 'react-router'

function NotFoundPage() {
  return (
    <>
      <h1 className="text-2xl font-semibold">Page not found</h1>
      <p className="mt-2 text-slate-700">
        There is no page at this address.{' '}
        <Link to="/" className="text-blue-800 underline underline-offset-4">
          Go to the home page
        </Link>
      </p>
    </>
  )
}

export default NotFoundPage
