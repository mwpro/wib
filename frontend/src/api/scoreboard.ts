import { request } from './client'
import type { AuthContextValue } from '../auth/context'
import type { ScoreboardResponse } from '../types/scoreboard'

export const fetchScoreboard = (auth: AuthContextValue, year: number, month: number) =>
  request<ScoreboardResponse>(auth, `/scoreboard/${year}/${month}`, {
    failureMessage: 'Nie udało się pobrać tabeli wyników.',
  })
