import { useEffect, useState } from 'react'

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
    <main>
      <h1>QualiTrack</h1>
      <p role="status">API status: {status}</p>
    </main>
  )
}

export default App
