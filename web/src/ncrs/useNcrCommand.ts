import { useMutation, useQueryClient } from '@tanstack/react-query'
import { allNcrsQueryKey } from '../api/ncrs.ts'

type CommandOptions = {
  onError?: (error: Error) => void
}

/**
 * Runs a command on an NCR. Once it succeeds, every cached NCR and list page is refetched, because the command
 * may have changed the NCR's status, its place in the list and what the page offers next.
 */
export function useNcrCommand<Input>(command: (input: Input) => Promise<void>, { onError }: CommandOptions = {}) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: command,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: allNcrsQueryKey }),
    onError,
  })
}
