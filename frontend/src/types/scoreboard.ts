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

export interface ChoreReviewItem {
  choreId: number
  title: string
  qualityScore: number
  onTimeCount: number
  lateCount: number
  isCurrentlyBad: boolean
  isArchived: boolean
}

export interface ChoreReviewResponse {
  year: number
  month: number
  isInProgress: boolean
  needsImprovement: ChoreReviewItem[]
  wellDone: ChoreReviewItem[]
}
