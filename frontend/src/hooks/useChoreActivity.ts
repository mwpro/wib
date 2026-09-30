import { useInfiniteQuery } from '@tanstack/react-query'
import { useAuth } from '../auth/useAuth'
import { fetchChoreCompletions } from '../api/choreCompletions'
import type { ChoreCompletionsResponse } from '../types/choreCompletion'

export function useChoreActivity(year: number, month: number, pageSize: number = 10) {
  const auth = useAuth()
  const { isAuthenticated } = auth

  return useInfiniteQuery<ChoreCompletionsResponse>({
    queryKey: ['chore-activity', year, month, pageSize],
    queryFn: ({ pageParam = 1 }) =>
      fetchChoreCompletions(auth, year, month, pageParam as number, pageSize),
    initialPageParam: 1,
    getNextPageParam: (lastPage) => (lastPage.hasMore ? lastPage.page + 1 : undefined),
    enabled: isAuthenticated && year > 0 && month > 0,
  })
}
