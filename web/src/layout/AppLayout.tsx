import { NavLink, Outlet } from 'react-router'
import { useAuth } from '../auth/useAuth.ts'
import { roleLabels } from '../auth/roleLabels.ts'
import SignOutButton from '../auth/SignOutButton.tsx'

function navLinkClass({ isActive }: { isActive: boolean }) {
  return isActive ? 'font-semibold text-blue-800 underline underline-offset-4' : 'text-slate-700 hover:text-blue-800'
}

// One banner, one main and one contentinfo landmark on every page, so screen reader users can jump
// between them; each page brings its own <h1> inside main.
function AppLayout() {
  const auth = useAuth()

  return (
    <div className="flex min-h-screen flex-col bg-slate-50 text-slate-900">
      <a
        href="#main-content"
        className="sr-only focus:not-sr-only focus:absolute focus:top-4 focus:left-4 focus:z-10 focus:rounded focus:bg-white focus:px-4 focus:py-2 focus:font-medium focus:text-blue-800 focus:shadow focus:outline-2 focus:outline-blue-700"
      >
        Skip to main content
      </a>

      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-5xl flex-wrap items-center gap-x-6 gap-y-2 px-4 py-3">
          <p className="text-lg font-semibold tracking-tight">QualiTrack</p>

          {auth.status === 'signedIn' && (
            <>
              <nav aria-label="Main">
                <ul className="flex gap-4">
                  <li>
                    <NavLink to="/" end className={navLinkClass}>
                      Home
                    </NavLink>
                  </li>
                  {auth.user.role === 'Inspector' && (
                    <li>
                      <NavLink to="/ncrs/new" className={navLinkClass}>
                        Raise an NCR
                      </NavLink>
                    </li>
                  )}
                </ul>
              </nav>
              <div className="flex flex-wrap items-center gap-x-4 gap-y-1 sm:ml-auto">
                <p className="text-sm text-slate-600">
                  {auth.user.email} ({roleLabels[auth.user.role]})
                </p>
                <SignOutButton />
              </div>
            </>
          )}
        </div>
      </header>

      {/* tabIndex -1 lets the skip link move keyboard focus here, not just scroll, in every browser */}
      <main
        id="main-content"
        tabIndex={-1}
        className="mx-auto w-full max-w-5xl flex-1 px-4 py-6 focus:outline-hidden"
      >
        <Outlet />
      </main>

      <footer className="border-t border-slate-200 bg-white">
        <div className="mx-auto max-w-5xl px-4 py-3 text-sm text-slate-600">
          QualiTrack · Non-conformance tracking
        </div>
      </footer>
    </div>
  )
}

export default AppLayout
