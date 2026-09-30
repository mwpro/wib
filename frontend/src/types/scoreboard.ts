export interface MemberScoreboardItem {
  memberId: number
  name: string
  monthlyPoints: number
  monthlyChoresCompleted: number
  workSharePercentage: number
  lifetimePoints: number
  lifetimeChoresCompleted: number
  rank: number
}

export interface ScoreboardResponse {
  year: number
  month: number
  periodStartUtc: string
  periodEndUtc: string
  totalHouseholdChoresCompleted: number
  totalHouseholdPointsEarned: number
  members: MemberScoreboardItem[]
}
