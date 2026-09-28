import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useAuth } from '../auth/useAuth'
import {
  completeChoreApi,
  createChoreApi,
  deleteChoreApi,
  fetchChores,
  updateChoreApi,
} from '../api/chores'
import type {
  ChoreResponse,
  CompleteChoreResponse,
  CreateChoreRequest,
  UpdateChoreRequest,
} from '../types/chore'
import type { Member } from '../types/member'

export function useChores() {
  const auth = useAuth()
  const { isAuthenticated } = auth

  return useQuery<ChoreResponse[]>({
    queryKey: ['chores'],
    queryFn: () => fetchChores(auth),
    enabled: isAuthenticated,
  })
}

export function useCompleteChore() {
  const auth = useAuth()
  const queryClient = useQueryClient()
  const memberKey = ['currentMember', auth.user?.sub]

  return useMutation<
    CompleteChoreResponse,
    Error,
    { choreId: number; points: number },
    { previousChores?: ChoreResponse[]; previousMember?: Member | null }
  >({
    mutationFn: ({ choreId }) => completeChoreApi(auth, choreId),
    onMutate: async ({ choreId, points }) => {
      await queryClient.cancelQueries({ queryKey: ['chores'] })
      await queryClient.cancelQueries({ queryKey: memberKey })

      const previousChores = queryClient.getQueryData<ChoreResponse[]>(['chores'])
      const previousMember = queryClient.getQueryData<Member | null>(memberKey)

      // Optimistically update chore in cache
      queryClient.setQueryData<ChoreResponse[]>(['chores'], (old) => {
        if (!old) return old
        return old.map((chore) => {
          if (chore.id !== choreId) return chore
          return {
            ...chore,
            urgency: 'Fresh' as const,
            urgencyRatio: 0,
            daysSinceLastDone: 0,
            lastCompletedAt: new Date().toISOString(),
          }
        })
      })

      // Optimistically update wallet balance in Header
      if (previousMember) {
        queryClient.setQueryData<Member | null>(memberKey, (old) => {
          if (!old) return old
          return {
            ...old,
            walletBalance: old.walletBalance + points,
          }
        })
      }

      return { previousChores, previousMember }
    },
    onError: (_err, _variables, context) => {
      if (context?.previousChores) {
        queryClient.setQueryData(['chores'], context.previousChores)
      }
      if (context?.previousMember !== undefined) {
        queryClient.setQueryData(memberKey, context.previousMember)
      }
    },
    onSuccess: (data) => {
      // Update with confirmed chore and balance
      queryClient.setQueryData<ChoreResponse[]>(['chores'], (old) => {
        if (!old) return [data.chore]
        return old.map((c) => (c.id === data.chore.id ? data.chore : c))
      })

      queryClient.setQueryData<Member | null>(memberKey, (old) => {
        if (!old) return old
        return {
          ...old,
          walletBalance: data.memberWalletBalance,
        }
      })
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['chores'] })
      queryClient.invalidateQueries({ queryKey: memberKey })
    },
  })
}

export function useCreateChore() {
  const auth = useAuth()
  const queryClient = useQueryClient()

  return useMutation<ChoreResponse, Error, CreateChoreRequest>({
    mutationFn: (req) => createChoreApi(auth, req),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['chores'] })
    },
  })
}

export function useUpdateChore() {
  const auth = useAuth()
  const queryClient = useQueryClient()

  return useMutation<ChoreResponse, Error, { choreId: number; req: UpdateChoreRequest }>({
    mutationFn: ({ choreId, req }) => updateChoreApi(auth, choreId, req),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['chores'] })
    },
  })
}

export function useDeleteChore() {
  const auth = useAuth()
  const queryClient = useQueryClient()

  return useMutation<void, Error, number>({
    mutationFn: (choreId) => deleteChoreApi(auth, choreId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['chores'] })
    },
  })
}
