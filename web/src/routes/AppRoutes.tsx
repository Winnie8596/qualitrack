import { Route, Routes } from 'react-router'
import ProtectedRoute from '../auth/ProtectedRoute.tsx'
import AppLayout from '../layout/AppLayout.tsx'
import DashboardPage from '../pages/DashboardPage.tsx'
import HomePage from '../pages/HomePage.tsx'
import LoginPage from '../pages/LoginPage.tsx'
import NcrDetailPage from '../pages/NcrDetailPage.tsx'
import NcrListPage from '../pages/NcrListPage.tsx'
import NotFoundPage from '../pages/NotFoundPage.tsx'
import RaiseNcrPage from '../pages/RaiseNcrPage.tsx'

// Who may open each page (PRD §4). The API enforces the same rules; this only spares users pages they cannot use.
function AppRoutes() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route path="login" element={<LoginPage />} />

        <Route element={<ProtectedRoute />}>
          <Route index element={<HomePage />} />
          <Route path="ncrs" element={<NcrListPage />} />
          <Route path="ncrs/:id" element={<NcrDetailPage />} />
        </Route>

        <Route element={<ProtectedRoute allow={['Inspector']} />}>
          <Route path="ncrs/new" element={<RaiseNcrPage />} />
        </Route>

        <Route element={<ProtectedRoute allow={['QaManager']} />}>
          <Route path="dashboard" element={<DashboardPage />} />
        </Route>

        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  )
}

export default AppRoutes
