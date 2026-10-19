import { fetchWithAuth } from './client'
import { apiErrorMessage } from './errors'
import type { AuthContextValue } from '../auth/context'
import type { VoucherResponse } from '../types/voucher'

export async function fetchVouchers(
  auth: AuthContextValue,
  isRedeemed?: boolean
): Promise<VoucherResponse[]> {
  const url =
    isRedeemed !== undefined
      ? `/api/vouchers?isRedeemed=${isRedeemed}`
      : '/api/vouchers'

  const response = await fetchWithAuth(url, auth)
  if (!response.ok) {
    throw new Error(`Nie udało się pobrać kuponów: ${response.statusText}`)
  }
  return response.json()
}

export async function redeemVoucherApi(
  auth: AuthContextValue,
  id: number
): Promise<VoucherResponse> {
  const response = await fetchWithAuth(`/api/vouchers/${id}/redemption`, auth, {
    method: 'POST',
  })
  if (!response.ok) {
    throw new Error(await apiErrorMessage(response, 'Nie udało się zrealizować kuponu.'))
  }
  return response.json()
}
