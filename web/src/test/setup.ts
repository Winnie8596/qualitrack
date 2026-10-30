import '@testing-library/jest-dom/vitest'
import { cleanup, configure } from '@testing-library/react'
import { afterEach } from 'vitest'

// Page tests render the whole app and type like a user, and test files run in parallel. Under that load (and on
// two-core CI runners) the defaults of 1 s to find an element are too tight; waiting longer changes no assertion.
configure({ asyncUtilTimeout: 3000 })

afterEach(cleanup)
