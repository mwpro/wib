import type { VoucherResponse } from './voucher'

export interface RewardItemResponse {
  id: number
  title: string
  description?: string | null
  pointCost: number
  quantity?: number | null
  isActive: boolean
  createdByMemberId: number
  createdAt: string
  updatedAt?: string | null
}

export interface CreateRewardItemRequest {
  title: string
  pointCost: number
  description?: string | null
  quantity?: number | null
}

export interface UpdateRewardItemRequest {
  title: string
  pointCost: number
  description?: string | null
  quantity?: number | null
}

export interface BuyRewardResponse {
  voucher: VoucherResponse
  memberWalletBalance: number
}
