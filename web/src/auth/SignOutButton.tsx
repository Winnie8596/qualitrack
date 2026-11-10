import { useMutation, useQueryClient } from '@tanstack/react-query'
import { signOut } from '../api/auth.ts'
import { rememberSignedOut } from './sessionCache.ts'

// Signed out only once the API says so: if the request fails, the cookie still works, so the page says that.
function SignOutButton() {
  const queryClient = useQueryClient()
  const logout = useMutation({
    mutationFn: signOut,
    onSuccess: () => rememberSignedOut(queryClient),
  })

  return (
    <>
      <button
        type="button"
        onClick={() => logout.mutate()}
        disabled={logout.isPending}
        className="text-sm font-medium text-blue-800 underline underline-offset-4 hover:text-blue-950 disabled:opacity-60"
      >
        Sign out
      </button>
      {logout.isError && (
        <p role="alert" className="text-sm text-red-700">
          Sign-out failed. Try again.
        </p>
      )}
    </>
  )
}

export default SignOutButton
