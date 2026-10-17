export type FreshnessUrgency = 'Fresh' | 'DueSoon' | 'Overdue' | 'Neglected' | 'Unscheduled'

/** A completion outcome that was judged against a cadence. */
export type JudgedOutcome = Exclude<FreshnessUrgency, 'Unscheduled'>

export interface ChoreResponse {
  id: number
  title: string
  description?: string | null
  points: number
  cadenceDays?: number | null
  lastCompletedAt?: string | null
  tags: string[]
  isArchived: boolean
  urgency: FreshnessUrgency
  urgencyRatio?: number | null
  daysSinceLastDone?: number | null
  createdAt: string
  updatedAt?: string | null
}

export interface CompleteChoreResponse {
  chore: ChoreResponse
  memberWalletBalance: number
  pointsAwarded: number
}

export interface CreateChoreRequest {
  title: string
  description?: string | null
  points?: number
  cadenceDays?: number | null
  tags?: string[]
}

export interface UpdateChoreRequest {
  title: string
  description?: string | null
  points?: number
  cadenceDays?: number | null
  tags?: string[]
}

export interface ChoreHistoryItem {
  id: number
  memberId: number
  memberName: string
  pointsAwarded: number
  completedAt: string
  daysSincePrevious: number
  urgencyAtCompletion: FreshnessUrgency
}

export interface ChoreHistoryMemberShare {
  memberId: number
  memberName: string
  count: number
}

export interface ChoreHistorySummary {
  totalCount: number
  outcomeCounts: Record<JudgedOutcome, number>
  onTimeCount: number
  members: ChoreHistoryMemberShare[]
}

export interface ChoreHistoryResponse {
  items: ChoreHistoryItem[]
  page: number
  pageSize: number
  totalCount: number
  hasMore: boolean
}
