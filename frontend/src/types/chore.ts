export type FreshnessUrgency = 'Fresh' | 'DueSoon' | 'Overdue' | 'Neglected' | 'Unscheduled'

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
