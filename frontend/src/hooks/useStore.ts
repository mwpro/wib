import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useAuth } from '../auth/useAuth'
import {
  fetchStoreItems,
  createStoreItemApi,
  updateStoreItemApi,
  deleteStoreItemApi,
  purchaseStoreItemApi,
} from '../api/store'
import type {
  RewardItemResponse,
  CreateRewardItemRequest,
  UpdateRewardItemRequest,
  BuyRewardResponse,
} from '../types/store'
import type { Member } from '../types/member'

export function useStoreItems() {
  const auth = useAuth()
  const { isAuthenticated } = auth

  return useQuery<RewardItemResponse[]>({
    queryKey: ['store', 'items'],
    queryFn: () => fetchStoreItems(auth),
    enabled: isAuthenticated,
  })
}

export function usePurchaseReward() {
  const auth = useAuth()
  const queryClient = useQueryClient()
  const memberKey = ['currentMember', auth.user?.sub]

  return useMutation<
    BuyRewardResponse,
    Error,
    { rewardItemId: number; pointCost: number },
    { previousMember?: Member | null }
  >({
    mutationFn: ({ rewardItemId }) => purchaseStoreItemApi(auth, rewardItemId),
    onMutate: async ({ pointCost }) => {
      await queryClient.cancelQueries({ queryKey: memberKey })
      const previousMember = queryClient.getQueryData<Member | null>(memberKey)

      if (previousMember) {
        queryClient.setQueryData<Member | null>(memberKey, (old) => {
          if (!old) return old
          return {
            ...old,
            walletBalance: Math.max(0, old.walletBalance - pointCost),
          }
        })
      }

      return { previousMember }
    },
    onError: (_err, _vars, context) => {
      if (context?.previousMember !== undefined) {
        queryClient.setQueryData(memberKey, context.previousMember)
      }
    },
    onSuccess: (data) => {
      // Set confirmed balance from server
      queryClient.setQueryData<Member | null>(memberKey, (old) => {
        if (!old) return old
        return {
          ...old,
          walletBalance: data.memberWalletBalance,
        }
      })

      // Invalidate store items (quantity may have changed or deactivated)
      queryClient.invalidateQueries({ queryKey: ['store', 'items'] })
      // Invalidate vouchers so active vouchers show the new purchase
      queryClient.invalidateQueries({ queryKey: ['vouchers'] })
    },
  })
}

export function useCreateRewardItem() {
  const auth = useAuth()
  const queryClient = useQueryClient()

  return useMutation<RewardItemResponse, Error, CreateRewardItemRequest>({
    mutationFn: (req) => createStoreItemApi(auth, req),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['store', 'items'] })
    },
  })
}

export function useUpdateRewardItem() {
  const auth = useAuth()
  const queryClient = useQueryClient()

  return useMutation<RewardItemResponse, Error, { id: number; req: UpdateRewardItemRequest }>({
    mutationFn: ({ id, req }) => updateStoreItemApi(auth, id, req),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['store', 'items'] })
    },
  })
}

export function useDeleteRewardItem() {
  const auth = useAuth()
  const queryClient = useQueryClient()

  return useMutation<void, Error, number>({
    mutationFn: (id) => deleteStoreItemApi(auth, id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['store', 'items'] })
    },
  })
}
