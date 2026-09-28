import { useQuery } from '@tanstack/react-query'
import { useAuth } from '../auth/useAuth'
import { getCurrentMember } from '../api/client'
import type { Member } from '../types/member'

export function useCurrentMember() {
  const auth = useAuth()
  const { isAuthenticated, user } = auth

  const query = useQuery<Member | null>({
    queryKey: ['currentMember', user?.sub],
    queryFn: () => getCurrentMember(auth),
    enabled: isAuthenticated && Boolean(user?.sub),
    staleTime: 1000 * 60, // 1 minute
  })

  return {
    currentMember: isAuthenticated ? (query.data ?? null) : null,
    isLoading: query.isLoading,
    refetch: query.refetch,
  }
}
