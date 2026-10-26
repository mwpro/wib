import { useState } from 'react'
import { AlertCircle, Award, ClipboardCheck, Loader2, TrendingDown } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { useChoreReview } from '../../hooks/useChoreReview'
import { ChoreHistoryModal } from '../chores/ChoreHistoryModal'
import type { ChoreResponse } from '../../types/chore'
import type { ChoreReviewItem } from '../../types/scoreboard'

interface ChoreReviewProps {
  year: number
  month: number
}

const VISIBLE_ITEMS = 5

type ChoreRef = Pick<ChoreResponse, 'id' | 'title'>

function describe(item: ChoreReviewItem, isInProgress: boolean): string {
  const parts: string[] = []
  if (item.onTimeCount > 0) parts.push(`${item.onTimeCount}× na czas`)
  if (item.lateCount > 0) parts.push(`${item.lateCount}× z opóźnieniem`)
  if (item.isCurrentlyBad) parts.push(isInProgress ? 'zaległe teraz' : 'zaległe na koniec miesiąca')
  if (item.isArchived) parts.push('zarchiwizowane')
  return parts.join(' · ')
}

interface ReviewListProps {
  testId: string
  title: string
  icon: LucideIcon
  iconClass: string
  scoreClass: string
  items: ChoreReviewItem[]
  isInProgress: boolean
  onSelect: (chore: ChoreRef) => void
}

function ReviewList({
  testId,
  title,
  icon: Icon,
  iconClass,
  scoreClass,
  items,
  isInProgress,
  onSelect,
}: ReviewListProps) {
  const [expanded, setExpanded] = useState(false)
  const visible = expanded ? items : items.slice(0, VISIBLE_ITEMS)

  return (
    <div data-testid={testId} className="flex flex-col gap-1.5">
      <div className="flex items-center gap-1.5">
        <Icon className={`h-3.5 w-3.5 ${iconClass}`} />
        <h4 className="text-xs font-semibold text-stone-700 dark:text-stone-300">{title}</h4>
        {items.length > 0 && (
          <span className="text-[11px] text-stone-400 dark:text-stone-500">({items.length})</span>
        )}
      </div>

      {items.length === 0 ? (
        <div className="text-xs text-stone-400 dark:text-stone-500 py-3 text-center italic">
          Brak pasujących obowiązków.
        </div>
      ) : (
        <div className="flex flex-col divide-y divide-stone-100 dark:divide-stone-800/60">
          {visible.map((item) => {
            const content = (
              <>
                <div className="flex flex-col min-w-0 text-left">
                  <span className="font-medium text-stone-900 dark:text-stone-100 truncate">
                    {item.title}
                  </span>
                  <span className="text-[11px] text-stone-400 dark:text-stone-500">
                    {describe(item, isInProgress)}
                  </span>
                </div>
                <span className={`font-bold tabular-nums shrink-0 ${scoreClass}`}>
                  {Math.floor(item.qualityScore * 100)}%
                </span>
              </>
            )
            const rowClass = 'py-2 flex items-center justify-between gap-3 text-xs w-full'

            return item.isArchived ? (
              <div key={item.choreId} data-testid={`review-item-${item.choreId}`} className={rowClass}>
                {content}
              </div>
            ) : (
              <button
                key={item.choreId}
                type="button"
                data-testid={`review-item-${item.choreId}`}
                onClick={() => onSelect({ id: item.choreId, title: item.title })}
                className={`${rowClass} cursor-pointer rounded-lg hover:bg-stone-50 dark:hover:bg-stone-800/50 transition px-1 -mx-1`}
              >
                {content}
              </button>
            )
          })}

          {items.length > VISIBLE_ITEMS && (
            <div className="pt-2 flex justify-center">
              <button
                type="button"
                onClick={() => setExpanded((e) => !e)}
                className="px-3 py-1 rounded-xl border border-stone-200 dark:border-stone-700 bg-stone-50 dark:bg-stone-800/80 hover:bg-stone-100 dark:hover:bg-stone-700 text-stone-700 dark:text-stone-200 text-[11px] font-semibold transition cursor-pointer"
              >
                {expanded ? 'Pokaż mniej' : `Pokaż wszystkie (${items.length})`}
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  )
}

export function ChoreReview({ year, month }: ChoreReviewProps) {
  const { data, isLoading, isError, error, refetch } = useChoreReview(year, month)
  const [choreForHistory, setChoreForHistory] = useState<ChoreRef | null>(null)

  return (
    <div
      data-testid="chore-review"
      className="bg-white dark:bg-[#14161d] border border-stone-200/90 dark:border-stone-800/80 rounded-2xl p-4 shadow-xs flex flex-col gap-3"
    >
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <ClipboardCheck className="h-4 w-4 text-amber-600 dark:text-amber-400" />
          <h3 className="text-xs font-semibold uppercase tracking-wider text-stone-500 dark:text-stone-400">
            Przegląd obowiązków
          </h3>
        </div>
        {data?.isInProgress && (
          <span className="text-[11px] font-semibold text-amber-700 dark:text-amber-400">w toku</span>
        )}
      </div>

      {isLoading ? (
        <div className="flex items-center justify-center py-8 text-stone-400 dark:text-stone-500 text-xs gap-2">
          <Loader2 className="h-4 w-4 animate-spin" />
          <span>Wczytywanie przeglądu...</span>
        </div>
      ) : isError || !data ? (
        <div className="p-4 rounded-xl bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-900 text-rose-800 dark:text-rose-200 flex items-center justify-between gap-3 text-xs">
          <div className="flex items-center gap-2 min-w-0">
            <AlertCircle className="h-4 w-4 text-rose-500 shrink-0" />
            <span className="truncate">
              {error instanceof Error ? error.message : 'Nie udało się wczytać przeglądu.'}
            </span>
          </div>
          <button
            type="button"
            onClick={() => refetch()}
            className="px-3 py-1 bg-rose-100 dark:bg-rose-900/60 hover:bg-rose-200 dark:hover:bg-rose-900 rounded-lg text-[11px] font-semibold text-rose-900 dark:text-rose-100 transition cursor-pointer shrink-0"
          >
            Spróbuj ponownie
          </button>
        </div>
      ) : (
        <div key={`${year}-${month}`} className="flex flex-col gap-4">
          <ReviewList
            testId="review-well-done"
            title="Dobra robota"
            icon={Award}
            iconClass="text-emerald-500 dark:text-emerald-400"
            scoreClass="text-emerald-600 dark:text-emerald-400"
            items={data.wellDone}
            isInProgress={data.isInProgress}
            onSelect={setChoreForHistory}
          />
          <ReviewList
            testId="review-needs-improvement"
            title="Do poprawy"
            icon={TrendingDown}
            iconClass="text-rose-500 dark:text-rose-400"
            scoreClass="text-rose-600 dark:text-rose-400"
            items={data.needsImprovement}
            isInProgress={data.isInProgress}
            onSelect={setChoreForHistory}
          />
        </div>
      )}

      <ChoreHistoryModal chore={choreForHistory} onClose={() => setChoreForHistory(null)} />
    </div>
  )
}
