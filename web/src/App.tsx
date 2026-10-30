import { BrowserRouter } from 'react-router'
import AuthProvider from './auth/AuthProvider.tsx'
import AppRoutes from './routes/AppRoutes.tsx'

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <AppRoutes />
      </AuthProvider>
    </BrowserRouter>
  )
}

export default App
