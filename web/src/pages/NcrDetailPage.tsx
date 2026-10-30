import { useParams } from 'react-router'
import { z } from 'zod'
import { useAuth } from '../auth/useAuth.ts'
import NcrDetail from '../ncrs/NcrDetail.tsx'
import NcrNotFound from '../ncrs/NcrNotFound.tsx'

const idSchema = z.guid()

// Route params are plain strings (ADR 0008): one that cannot be an NCR id is not found, without asking the API.
function NcrDetailPage() {
  const id = idSchema.safeParse(useParams().id)
  const auth = useAuth()

  if (!id.success) {
    return <NcrNotFound />
  }
  // ProtectedRoute only renders this page for a signed-in user
  if (auth.status !== 'signedIn') {
    return null
  }
  return <NcrDetail id={id.data} currentUserId={auth.user.id} />
}

export default NcrDetailPage
