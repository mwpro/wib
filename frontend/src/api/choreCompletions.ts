import { request } from './client'
import type { AuthContextValue } from '../auth/context'
import type { ChoreCompletionsResponse } from '../types/choreCompletion'

export const fetchChoreCompletions = (
  auth: AuthContextValue,
  year: number,
  month: number,
  page: number = 1,
  pageSize: number = 10
) =>
  request<ChoreCompletionsResponse>(auth, `/chore-completions/${year}/${month}`, {
    query: { page, pageSize },
    failureMessage: 'Nie udało się pobrać aktywności zadań.',
  })
