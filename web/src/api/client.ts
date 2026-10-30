import { z } from 'zod'

// RFC 9457, as the API sends it. Validation problems add `errors`, keyed by the JSON field name.
export type ProblemDetails = {
  type?: string
  title?: string
  status?: number
  detail?: string
  errors?: Record<string, string[]>
}

const problemDetailsSchema = z.object({
  type: z.string().optional(),
  title: z.string().optional(),
  status: z.number().optional(),
  detail: z.string().optional(),
  errors: z.record(z.string(), z.array(z.string())).optional(),
})

/** The API answered, and the answer was a failure. Network failures stay as the TypeError fetch throws. */
export class ApiError extends Error {
  readonly status: number
  readonly problem: ProblemDetails

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? `Request failed with status ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}

async function problemFrom(response: Response): Promise<ProblemDetails> {
  try {
    const parsed = problemDetailsSchema.safeParse(await response.json())
    return parsed.success ? parsed.data : {}
  } catch {
    // A proxy or crash page, not ProblemDetails; the status code still says what happened
    return {}
  }
}

async function send(path: string, init: RequestInit): Promise<Response> {
  const response = await fetch(path, init)
  if (!response.ok) {
    throw new ApiError(response.status, await problemFrom(response))
  }
  return response
}

// The body is checked against the schema, so a change in the API's contract fails here, loudly, and not as
// undefined somewhere in a page.
async function parse<T>(response: Response, schema: z.ZodType<T>): Promise<T> {
  return schema.parse(await response.json())
}

export async function getJson<T>(path: string, schema: z.ZodType<T>, signal?: AbortSignal): Promise<T> {
  return parse(await send(path, { signal }), schema)
}

const antiforgeryTokenSchema = z.object({ token: z.string() })

let antiforgeryToken: Promise<string> | undefined

// One token per signed-in user (ADR 0006), so it is fetched on the first write and reused until forgotten
function currentAntiforgeryToken(): Promise<string> {
  antiforgeryToken ??= getJson('/api/auth/antiforgery', antiforgeryTokenSchema).then(
    ({ token }) => token,
    (error: unknown) => {
      antiforgeryToken = undefined
      throw error
    },
  )
  return antiforgeryToken
}

/** The token belongs to the user who fetched it; call this whenever that changes (sign in, sign out). */
export function forgetAntiforgeryToken(): void {
  antiforgeryToken = undefined
}

function post(path: string, body: unknown, headers: Record<string, string> = {}): Promise<Response> {
  return send(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...headers },
    body: JSON.stringify(body),
  })
}

/** A write as the signed-in user, carrying the antiforgery token the API requires. */
export async function postJson<T>(path: string, body: unknown, schema: z.ZodType<T>): Promise<T> {
  return parse(await post(path, body, { 'X-XSRF-TOKEN': await currentAntiforgeryToken() }), schema)
}

/** A command as the signed-in user: the API answers 204 with no body, so there is nothing to parse. */
export async function postCommand(path: string, body: unknown): Promise<void> {
  await post(path, body, { 'X-XSRF-TOKEN': await currentAntiforgeryToken() })
}

// Signing in and out only: the API asks no token of them, and before sign-in there is no user to fetch one for.

export async function postJsonWithoutAntiforgery<T>(path: string, body: unknown, schema: z.ZodType<T>): Promise<T> {
  return parse(await post(path, body), schema)
}

export async function postWithoutAntiforgery(path: string): Promise<void> {
  await post(path, {})
}
