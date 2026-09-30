export interface ChoreCompletionItem {
  id: number
  choreId: number
  choreTitle: string
  memberId: number
  memberName: string
  pointsAwarded: number
  completedAt: string
}

export interface ChoreCompletionsResponse {
  items: ChoreCompletionItem[]
  page: number
  pageSize: number
  totalCount: number
  hasMore: boolean
}
