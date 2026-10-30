import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { Navigate, useLocation } from 'react-router'
import { z } from 'zod'
import { signIn } from '../api/auth.ts'
import { toFormErrors } from '../api/formErrors.ts'
import { rememberSignedIn } from '../auth/sessionCache.ts'
import { useAuth } from '../auth/useAuth.ts'
import { primaryButtonClass } from '../forms/fieldClass.ts'
import FormAlert from '../forms/FormAlert.tsx'
import TextField from '../forms/TextField.tsx'

const loginSchema = z.object({
  email: z.string().trim().min(1, 'Enter your email.'),
  password: z.string().min(1, 'Enter your password.'),
})

type LoginForm = z.input<typeof loginSchema>

const loginFields = ['email', 'password'] as const

const fromSchema = z.object({ from: z.object({ pathname: z.string(), search: z.string() }) })

// Where ProtectedRoute was sending the user before it sent them here; the state is untyped, so it is checked.
function useReturnPath(): string {
  const parsed = fromSchema.safeParse(useLocation().state)
  return parsed.success ? parsed.data.from.pathname + parsed.data.from.search : '/'
}

function LoginPage() {
  const auth = useAuth()
  const returnPath = useReturnPath()
  const queryClient = useQueryClient()
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginForm, unknown, z.output<typeof loginSchema>>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  })
  const login = useMutation({
    mutationFn: signIn,
    onSuccess: (user) => rememberSignedIn(queryClient, user),
  })

  // Signing in updates the session, which lands here: one path for "just signed in" and "already signed in"
  if (auth.status === 'signedIn') {
    return <Navigate to={returnPath} replace />
  }

  const serverError = login.error ? toFormErrors(login.error, loginFields).form : undefined

  return (
    <div className="mx-auto max-w-sm">
      <h1 className="text-2xl font-semibold">Sign in</h1>
      <form noValidate onSubmit={handleSubmit((credentials) => login.mutate(credentials))} className="mt-6 space-y-4">
        {serverError && <FormAlert message={serverError} />}
        <TextField label="Email" type="email" autoComplete="username" registration={register('email')} error={errors.email?.message} />
        <TextField
          label="Password"
          type="password"
          autoComplete="current-password"
          registration={register('password')}
          error={errors.password?.message}
        />
        <button type="submit" disabled={login.isPending} className={primaryButtonClass}>
          {login.isPending ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
    </div>
  )
}

export default LoginPage
