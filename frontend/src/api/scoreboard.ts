import { request } from './client'
import type { AuthContextValue } from '../auth/context'
import type { ChoreReviewResponse, ScoreboardResponse } from '../types/scoreboard'

export const fetchScoreboard = (auth: AuthContextValue, year: number, month: number) =>
  request<ScoreboardResponse>(auth, `/analytics/${year}/${month}/scoreboard`, {
    failureMessage: 'Nie udało się pobrać tabeli wyników.',
  })

export const fetchChoreReview = (auth: AuthContextValue, year: number, month: number) =>
  request<ChoreReviewResponse>(auth, `/analytics/${year}/${month}/chore-review`, {
    failureMessage: 'Nie udało się pobrać przeglądu obowiązków.',
  })
