import { AlertCircle, History, Loader2, Sparkles } from 'lucide-react'
import { useChoreActivity } from '../../hooks/useChoreActivity'
import { formatRelativeTimePl } from '../../utils/relativeTime'

interface ActivityStreamProps {
  year: number
  month: number
}

export function ActivityStream({ year, month }: ActivityStreamProps) {
  const {
    data,
    isLoading,
    isError,
    error,
    refetch,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
  } = useChoreActivity(year, month, 10)

  const items = data ? data.pages.flatMap((page) => page.items) : []

  return (
    <div
      data-testid="activity-stream"
      className="bg-white dark:bg-[#14161d] border border-stone-200/90 dark:border-stone-800/80 rounded-2xl p-4 shadow-xs flex flex-col gap-3"
    >
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <History className="h-4 w-4 text-emerald-600 dark:text-emerald-400" />
          <h3 className="text-xs font-semibold uppercase tracking-wider text-stone-500 dark:text-stone-400">
            Ostatnia aktywność
          </h3>
        </div>
        {items.length > 0 && (
          <span className="text-xs text-stone-400 dark:text-stone-500">
            {items.length} {items.length === 1 ? 'wpis' : 'wpisów'}
          </span>
        )}
      </div>

      {isLoading ? (
        <div className="flex items-center justify-center py-8 text-stone-400 dark:text-stone-500 text-xs gap-2">
          <Loader2 className="h-4 w-4 animate-spin" />
          <span>Wczytywanie aktywności...</span>
        </div>
      ) : isError ? (
        <div className="p-4 rounded-xl bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-900 text-rose-800 dark:text-rose-200 flex items-center justify-between gap-3 text-xs">
          <div className="flex items-center gap-2 min-w-0">
            <AlertCircle className="h-4 w-4 text-rose-500 shrink-0" />
            <span className="truncate">
              {error instanceof Error ? error.message : 'Nie udało się wczytać aktywności.'}
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
      ) : items.length === 0 ? (
        <div className="text-xs text-stone-400 dark:text-stone-500 py-6 text-center italic">
          Brak ukończonych zadań w tym miesiącu.
        </div>
      ) : (
        <div className="flex flex-col divide-y divide-stone-100 dark:divide-stone-800/60">
          {items.map((item) => (
            <div
              key={item.id}
              data-testid={`activity-item-${item.id}`}
              className="py-2.5 first:pt-1 last:pb-1 flex items-start justify-between gap-3 text-xs"
            >
              <div className="flex items-start gap-2.5 min-w-0">
                <div className="w-5 h-5 rounded-full bg-emerald-50 dark:bg-emerald-950/40 text-emerald-600 dark:text-emerald-400 flex items-center justify-center shrink-0 mt-0.5">
                  <Sparkles className="h-3 w-3" />
                </div>
                <div className="flex flex-col min-w-0">
                  <p className="text-stone-800 dark:text-stone-200 leading-snug">
                    <span className="font-semibold text-stone-900 dark:text-stone-100">
                      {item.memberName}
                    </span>{' '}
                    ukończył(a):{' '}
                    <span className="font-medium text-stone-800 dark:text-stone-200">
                      {item.choreTitle}
                    </span>{' '}
                    <span className="text-emerald-600 dark:text-emerald-400 font-semibold whitespace-nowrap">
                      (+{item.pointsAwarded} pkt)
                    </span>
                  </p>
                  <span className="text-[11px] text-stone-400 dark:text-stone-500 pt-0.5">
                    {formatRelativeTimePl(item.completedAt)}
                  </span>
                </div>
              </div>
            </div>
          ))}

          {hasNextPage && (
            <div className="pt-3 flex justify-center">
              <button
                type="button"
                onClick={() => fetchNextPage()}
                disabled={isFetchingNextPage}
                data-testid="show-more-activity-btn"
                className="px-4 py-1.5 rounded-xl border border-stone-200 dark:border-stone-700 bg-stone-50 dark:bg-stone-800/80 hover:bg-stone-100 dark:hover:bg-stone-700 text-stone-700 dark:text-stone-200 text-xs font-semibold transition cursor-pointer flex items-center gap-1.5 disabled:opacity-50"
              >
                {isFetchingNextPage ? (
                  <>
                    <Loader2 className="h-3.5 w-3.5 animate-spin" />
                    <span>Ładowanie...</span>
                  </>
                ) : (
                  <span>Pokaż więcej</span>
                )}
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  )
}
