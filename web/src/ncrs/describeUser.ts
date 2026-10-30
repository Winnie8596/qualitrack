// The API returns users as ids only. Until it returns names, the reader is "You" and anyone else a short id.
export function describeUser(userId: string, currentUserId: string): string {
  return userId === currentUserId ? 'You' : `User ${userId.slice(0, 8)}`
}
