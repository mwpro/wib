import { request } from './client'
import type { AuthContextValue } from '../auth/context'
import type {
  RewardItemResponse,
  CreateRewardItemRequest,
  UpdateRewardItemRequest,
  BuyRewardResponse,
} from '../types/store'

export const fetchStoreItems = (auth: AuthContextValue) =>
  request<RewardItemResponse[]>(auth, '/store/items', {
    failureMessage: 'Nie udało się pobrać listy nagród.',
  })

export const createStoreItemApi = (auth: AuthContextValue, req: CreateRewardItemRequest) =>
  request<RewardItemResponse>(auth, '/store/items', {
    method: 'POST',
    body: req,
    failureMessage: 'Nie udało się utworzyć nagrody.',
  })

export const updateStoreItemApi = (auth: AuthContextValue, id: number, req: UpdateRewardItemRequest) =>
  request<RewardItemResponse>(auth, `/store/items/${id}`, {
    method: 'PUT',
    body: req,
    failureMessage: 'Nie udało się zaktualizować nagrody.',
  })

export const deleteStoreItemApi = (auth: AuthContextValue, id: number) =>
  request<void>(auth, `/store/items/${id}`, {
    method: 'DELETE',
    failureMessage: 'Nie udało się usunąć nagrody ze sklepu.',
  })

export const purchaseStoreItemApi = (auth: AuthContextValue, id: number) =>
  request<BuyRewardResponse>(auth, `/store/items/${id}/purchase`, {
    method: 'POST',
    failureMessage: 'Nie udało się kupić nagrody.',
  })
