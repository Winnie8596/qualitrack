// eslint-disable-next-line no-restricted-imports -- the one place Zod itself is imported; see below
import { z } from 'zod'

// Zod makes object parsing faster by compiling a function at runtime, and tests whether it may by calling
// `new Function`. The API's Content-Security-Policy forbids exactly that (no 'unsafe-eval'), so the test fails
// harmlessly but the browser reports a violation on every page load. Telling Zod first keeps the console clean;
// the schemas here are small and parse one response at a time, so the slower path costs nothing visible.
//
// Every schema imports `z` from this module rather than from 'zod' (the linter enforces it), so this line has
// always run before the first schema is built, however the bundler orders its chunks.
z.config({ jitless: true })

export { z }
