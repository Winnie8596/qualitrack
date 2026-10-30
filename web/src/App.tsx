import { useEffect, useState } from 'react'
import AppLayout from './layout/AppLayout.tsx'

type ApiStatus = 'checking' | 'healthy' | 'unreachable'

function App() {
  const [status, setStatus] = useState<ApiStatus>('checking')

  useEffect(() => {
    const controller = new AbortController()

    fetch('/health', { signal: controller.signal })
      .then((response) => setStatus(response.ok ? 'healthy' : 'unreachable'))
      .catch((error: unknown) => {
        // Unmounting aborts the request; that is not an API failure.
        if (!controller.signal.aborted) {
          console.error('Health check failed', error)
          setStatus('unreachable')
        }
      })

    return () => controller.abort()
  }, [])

  return (
    <AppLayout>
      <h1 className="text-2xl font-semibold">QualiTrack</h1>
      <p role="status" className="mt-2 text-slate-700">
        API status: {status}
      </p>
    </AppLayout>
  )
}

export default App
