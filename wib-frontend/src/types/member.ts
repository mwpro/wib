export interface Member {
  id: number
  auth0UserId: string
  name: string
  email?: string
  picture?: string
  walletBalance: number
  earnedPoints: number
  createdAt: string
}
