import { lazy } from 'react'
import { Route, Routes } from 'react-router'
import ProtectedRoute from '../auth/ProtectedRoute.tsx'
import AppLayout from '../layout/AppLayout.tsx'

// One chunk per page, fetched the first time its route is opened (AppLayout shows a status while it loads),
// so the first visit downloads only the shell and the page it lands on, not every form in the app.
const DashboardPage = lazy(() => import('../pages/DashboardPage.tsx'))
const HomePage = lazy(() => import('../pages/HomePage.tsx'))
const LoginPage = lazy(() => import('../pages/LoginPage.tsx'))
const NcrDetailPage = lazy(() => import('../pages/NcrDetailPage.tsx'))
const NcrListPage = lazy(() => import('../pages/NcrListPage.tsx'))
const NotFoundPage = lazy(() => import('../pages/NotFoundPage.tsx'))
const RaiseNcrPage = lazy(() => import('../pages/RaiseNcrPage.tsx'))

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
