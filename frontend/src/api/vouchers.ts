import { request } from './client'
import type { AuthContextValue } from '../auth/context'
import type { VoucherResponse } from '../types/voucher'

export const fetchVouchers = (auth: AuthContextValue, isRedeemed?: boolean) =>
  request<VoucherResponse[]>(auth, '/vouchers', {
    query: { isRedeemed },
    failureMessage: 'Nie udało się pobrać kuponów.',
  })

export const redeemVoucherApi = (auth: AuthContextValue, id: number) =>
  request<VoucherResponse>(auth, `/vouchers/${id}/redemption`, {
    method: 'POST',
    failureMessage: 'Nie udało się zrealizować kuponu.',
  })
