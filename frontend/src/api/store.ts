import { fetchWithAuth } from './client'
import { apiErrorMessage } from './errors'
import type { AuthContextValue } from '../auth/context'
import type {
  RewardItemResponse,
  CreateRewardItemRequest,
  UpdateRewardItemRequest,
  BuyRewardResponse,
} from '../types/store'

export async function fetchStoreItems(
  auth: AuthContextValue
): Promise<RewardItemResponse[]> {
  const response = await fetchWithAuth('/api/store/items', auth)
  if (!response.ok) {
    throw new Error(`Nie udało się pobrać listy nagród: ${response.statusText}`)
  }
  return response.json()
}

export async function createStoreItemApi(
  auth: AuthContextValue,
  req: CreateRewardItemRequest
): Promise<RewardItemResponse> {
  const response = await fetchWithAuth('/api/store/items', auth, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(req),
  })
  if (!response.ok) {
    throw new Error(await apiErrorMessage(response, 'Nie udało się utworzyć nagrody.'))
  }
  return response.json()
}

export async function updateStoreItemApi(
  auth: AuthContextValue,
  id: number,
  req: UpdateRewardItemRequest
): Promise<RewardItemResponse> {
  const response = await fetchWithAuth(`/api/store/items/${id}`, auth, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(req),
  })
  if (!response.ok) {
    throw new Error(await apiErrorMessage(response, 'Nie udało się zaktualizować nagrody.'))
  }
  return response.json()
}

export async function deleteStoreItemApi(
  auth: AuthContextValue,
  id: number
): Promise<void> {
  const response = await fetchWithAuth(`/api/store/items/${id}`, auth, {
    method: 'DELETE',
  })
  if (!response.ok) {
    throw new Error(await apiErrorMessage(response, 'Nie udało się usunąć nagrody ze sklepu.'))
  }
}

export async function purchaseStoreItemApi(
  auth: AuthContextValue,
  id: number
): Promise<BuyRewardResponse> {
  const response = await fetchWithAuth(`/api/store/items/${id}/purchase`, auth, {
    method: 'POST',
  })
  if (!response.ok) {
    throw new Error(await apiErrorMessage(response, 'Nie udało się kupić nagrody.'))
  }
  return response.json()
}
