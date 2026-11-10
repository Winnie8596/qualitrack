import '@testing-library/jest-dom/vitest'
import { cleanup, configure } from '@testing-library/react'
import { afterEach } from 'vitest'

// Page tests render the whole app and type like a user, and test files run in parallel. Under that load (and on
// two-core CI runners) the defaults of 1 s to find an element are too tight; waiting longer changes no assertion.
configure({ asyncUtilTimeout: 3000 })

// The app loads each page lazily (AppRoutes). Loading them all here, before any test starts, means the first
// render of a page waits only for an already-loaded module, not for Vite to transform its whole import graph
// inside the 3 s above.
import.meta.glob('../pages/*Page.tsx', { eager: true })

afterEach(cleanup)
