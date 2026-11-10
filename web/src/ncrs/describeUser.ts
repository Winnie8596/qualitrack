// The API returns users as ids only. Until it returns names, the reader is "You" and anyone else a short id.
// The id's last characters, not its first: ids are time-ordered (version 7), so users created together share
// their opening characters and only the random tail tells them apart.
export function describeUser(userId: string, currentUserId: string): string {
  return userId === currentUserId ? 'You' : `User ${userId.slice(-8)}`
}
