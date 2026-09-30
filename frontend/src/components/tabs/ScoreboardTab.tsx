import { useState } from 'react'
import { AlertCircle, Loader2 } from 'lucide-react'
import { useScoreboard } from '../../hooks/useScoreboard'
import { MonthNavigator } from '../scoreboard/MonthNavigator'
import { WorkShareBar } from '../scoreboard/WorkShareBar'
import { MonthlyPodium } from '../scoreboard/MonthlyPodium'
import { ActivityStream } from '../scoreboard/ActivityStream'
import { LifetimeStats } from '../scoreboard/LifetimeStats'

export function ScoreboardTab() {
  const now = new Date()
  const [selectedYear, setSelectedYear] = useState(now.getFullYear())
  const [selectedMonth, setSelectedMonth] = useState(now.getMonth() + 1)

  const {
    data: scoreboard,
    isLoading,
    isError,
    error,
    refetch,
  } = useScoreboard(selectedYear, selectedMonth)

  const handleMonthChange = (year: number, month: number) => {
    setSelectedYear(year)
    setSelectedMonth(month)
  }

  return (
    <div className="flex flex-col gap-4">
      {/* Month Navigator */}
      <MonthNavigator
        year={selectedYear}
        month={selectedMonth}
        onChange={handleMonthChange}
      />

      {/* Loading state */}
      {isLoading && (
        <div className="p-12 flex flex-col items-center justify-center gap-3 text-stone-400">
          <Loader2 className="h-6 w-6 animate-spin text-amber-500" />
          <span className="text-sm font-medium">Ładowanie tabeli wyników...</span>
        </div>
      )}

      {/* Error state */}
      {isError && (
        <div className="p-6 rounded-2xl bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-900 text-rose-800 dark:text-rose-200 flex flex-col items-center gap-3 text-center">
          <AlertCircle className="h-8 w-8 text-rose-500" />
          <p className="text-sm font-medium">
            {error instanceof Error ? error.message : 'Wystąpił błąd podczas ładowania tabeli wyników.'}
          </p>
          <button
            type="button"
            onClick={() => refetch()}
            className="px-4 py-1.5 bg-rose-100 dark:bg-rose-900/60 hover:bg-rose-200 dark:hover:bg-rose-900 rounded-lg text-xs font-semibold text-rose-900 dark:text-rose-100 transition cursor-pointer"
          >
            Spróbuj ponownie
          </button>
        </div>
      )}

      {/* Main content when loaded and no error */}
      {!isLoading && !isError && scoreboard && (
        <>
          {/* Work Share Percentage Bar */}
          <WorkShareBar members={scoreboard.members} />

          {/* Monthly Podium (Ranked List) */}
          <MonthlyPodium members={scoreboard.members} />

          {/* Chronological Activity Stream */}
          <ActivityStream year={selectedYear} month={selectedMonth} />

          {/* Lifetime Statistics */}
          <LifetimeStats members={scoreboard.members} />
        </>
      )}
    </div>
  )
}
