export interface VoucherResponse {
  id: number
  rewardItemId: number
  titleSnapshot: string
  pointCostSnapshot: number
  ownedByMemberId: number
  isRedeemed: boolean
  purchasedAt: string
  redeemedAt?: string | null
}
