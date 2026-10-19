import { useMutation, useQuery } from '@tanstack/react-query'
import { useAuth } from '../auth/useAuth'
import {
  fetchStoreItems,
  createStoreItemApi,
  updateStoreItemApi,
  deleteStoreItemApi,
  purchaseStoreItemApi,
} from '../api/store'
import { queryKeys, useQueryCache, type Rollback } from './queryCache'
import type {
  RewardItemResponse,
  CreateRewardItemRequest,
  UpdateRewardItemRequest,
  BuyRewardResponse,
} from '../types/store'

export function useStoreItems() {
  const auth = useAuth()
  const { isAuthenticated } = auth

  return useQuery<RewardItemResponse[]>({
    queryKey: queryKeys.storeItems(),
    queryFn: () => fetchStoreItems(auth),
    enabled: isAuthenticated,
  })
}

export function usePurchaseReward() {
  const auth = useAuth()
  const cache = useQueryCache()

  return useMutation<
    BuyRewardResponse,
    Error,
    { rewardItemId: number; pointCost: number },
    Rollback
  >({
    mutationFn: ({ rewardItemId }) => purchaseStoreItemApi(auth, rewardItemId),
    onMutate: ({ pointCost }) => cache.rewardPurchaseStarted(pointCost),
    onError: (_err, _vars, rollback) => rollback?.(),
    onSuccess: (data) => cache.rewardPurchased(data),
  })
}

export function useCreateRewardItem() {
  const auth = useAuth()
  const cache = useQueryCache()

  return useMutation<RewardItemResponse, Error, CreateRewardItemRequest>({
    mutationFn: (req) => createStoreItemApi(auth, req),
    onSuccess: () => cache.rewardItemsChanged(),
  })
}

export function useUpdateRewardItem() {
  const auth = useAuth()
  const cache = useQueryCache()

  return useMutation<RewardItemResponse, Error, { id: number; req: UpdateRewardItemRequest }>({
    mutationFn: ({ id, req }) => updateStoreItemApi(auth, id, req),
    onSuccess: () => cache.rewardItemsChanged(),
  })
}

export function useDeleteRewardItem() {
  const auth = useAuth()
  const cache = useQueryCache()

  return useMutation<void, Error, number>({
    mutationFn: (id) => deleteStoreItemApi(auth, id),
    onSuccess: () => cache.rewardItemsChanged(),
  })
}
