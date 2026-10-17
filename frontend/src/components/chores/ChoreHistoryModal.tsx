import * as Dialog from '@radix-ui/react-dialog'
import { AlertCircle, History, Loader2, X } from 'lucide-react'
import { SegmentedBar } from '../common/SegmentedBar'
import { segmentColor } from '../common/segmentColors'
import { useChoreHistory, useChoreHistorySummary } from '../../hooks/useChoreHistory'
import type {
  ChoreHistoryItem,
  ChoreHistorySummary,
  ChoreResponse,
  JudgedOutcome,
} from '../../types/chore'

interface ChoreHistoryModalProps {
  chore: ChoreResponse | null
  onClose: () => void
}

// Unlike live freshness, both "early" and "on time" are good outcomes, so both read as positive.
// Declaration order is display order.
const OUTCOME_CONFIG: Record<JudgedOutcome, { label: string; badgeClass: string; barClass: string }> = {
  Fresh: {
    label: 'Przed czasem',
    badgeClass:
      'bg-sky-100 text-sky-950 dark:bg-sky-950/70 dark:text-sky-200 border-sky-300 dark:border-sky-800',
    barClass: 'bg-sky-500 dark:bg-sky-400',
  },
  DueSoon: {
    label: 'W terminie',
    badgeClass:
      'bg-emerald-100 text-emerald-900 dark:bg-emerald-950/70 dark:text-emerald-300 border-emerald-300 dark:border-emerald-800',
    barClass: 'bg-emerald-500 dark:bg-emerald-400',
  },
  Overdue: {
    label: 'Z opóźnieniem',
    badgeClass:
      'bg-orange-100 text-orange-950 dark:bg-orange-950/70 dark:text-orange-200 border-orange-300 dark:border-orange-800',
    barClass: 'bg-orange-500 dark:bg-orange-400',
  },
  Neglected: {
    label: 'Mocno spóźnione',
    badgeClass:
      'bg-rose-100 text-rose-950 dark:bg-rose-950/70 dark:text-rose-200 border-rose-300 dark:border-rose-800',
    barClass: 'bg-rose-500 dark:bg-rose-400',
  },
}

const OUTCOMES = Object.keys(OUTCOME_CONFIG) as JudgedOutcome[]

function OutcomeBadge({ outcome }: { outcome: JudgedOutcome }) {
  const config = OUTCOME_CONFIG[outcome]
  return (
    <span
      className={`inline-flex items-center px-2 py-0.5 rounded-md text-xs font-semibold border ${config.badgeClass}`}
    >
      {config.label}
    </span>
  )
}

const monthFormatter = new Intl.DateTimeFormat('pl-PL', {
  month: 'long',
  year: 'numeric',
  timeZone: 'Europe/Warsaw',
})

const dateTimeFormatter = new Intl.DateTimeFormat('pl-PL', {
  day: 'numeric',
  month: 'short',
  hour: '2-digit',
  minute: '2-digit',
  timeZone: 'Europe/Warsaw',
})

function formatMonth(date: Date): string {
  const label = monthFormatter.format(date)
  return label.charAt(0).toUpperCase() + label.slice(1)
}

function formatGap(days: number, isFirstEver: boolean): string {
  if (isFirstEver) {
    return days === 1 ? '1 dzień od utworzenia' : `${days} dni od utworzenia`
  }
  if (days === 0) return 'tego samego dnia'
  return days === 1 ? 'po 1 dniu' : `po ${days} dniach`
}

function groupByMonth(items: ChoreHistoryItem[]) {
  const groups: { label: string; items: ChoreHistoryItem[] }[] = []
  for (const item of items) {
    const label = formatMonth(new Date(item.completedAt))
    const last = groups[groups.length - 1]
    if (last && last.label === label) {
      last.items.push(item)
    } else {
      groups.push({ label, items: [item] })
    }
  }
  return groups
}

function HistorySummary({ summary }: { summary: ChoreHistorySummary }) {
  const judgedCount = OUTCOMES.reduce((sum, outcome) => sum + summary.outcomeCounts[outcome], 0)
  const onTimePercent = judgedCount > 0 ? Math.round((summary.onTimeCount / judgedCount) * 100) : null

  return (
    <div className="flex flex-col gap-3 pb-3.5 border-b border-stone-100 dark:border-stone-800">
      <div className="flex items-baseline justify-between gap-2">
        <span className="inline-flex items-baseline gap-1 text-sm text-stone-600 dark:text-stone-400">
          <span>Wykonano</span>
          <strong data-testid="history-total" className="text-stone-950 dark:text-white font-bold">
            {summary.totalCount}
          </strong>
          <span>{summary.totalCount === 1 ? 'raz' : 'razy'}</span>
        </span>
        {onTimePercent != null && (
          <span
            data-testid="history-on-time"
            className="text-sm font-bold text-emerald-700 dark:text-emerald-400"
          >
            {onTimePercent}% na czas
          </span>
        )}
      </div>

      {summary.members.length > 0 && (
        <SegmentedBar
          segments={summary.members.map((member, index) => ({
            key: String(member.memberId),
            label: member.memberName,
            value: member.count,
            barClass: segmentColor(index).bg,
            dotClass: segmentColor(index).indicator,
            testId: 'history-member-share',
          }))}
        />
      )}

      {judgedCount > 0 && (
        <SegmentedBar
          segments={OUTCOMES.map((outcome) => ({
            key: outcome,
            label: OUTCOME_CONFIG[outcome].label,
            value: summary.outcomeCounts[outcome],
            barClass: OUTCOME_CONFIG[outcome].barClass,
            dotClass: OUTCOME_CONFIG[outcome].barClass,
            testId: `history-outcome-${outcome}`,
          }))}
        />
      )}
    </div>
  )
}

export function ChoreHistoryModal({ chore, onClose }: ChoreHistoryModalProps) {
  const history = useChoreHistory(chore?.id ?? null)
  const { data: summary, ...summaryQuery } = useChoreHistorySummary(chore?.id ?? null)
  const { data, fetchNextPage, hasNextPage, isFetchingNextPage } = history
  const isLoading = history.isLoading || summaryQuery.isLoading
  const isError = history.isError || summaryQuery.isError

  if (!chore) return null

  const items = data?.pages.flatMap((p) => p.items) ?? []
  const oldestId = !hasNextPage && items.length > 0 ? items[items.length - 1].id : null

  return (
    <Dialog.Root open onOpenChange={(open) => !open && onClose()}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/60 z-40 backdrop-blur-xs animate-in fade-in" />
        <Dialog.Content
          data-testid="chore-history-modal"
          className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 z-50 w-[calc(100%-2rem)] max-w-lg bg-white dark:bg-[#14161d] rounded-2xl p-4 sm:p-6 shadow-2xl border border-stone-200/90 dark:border-stone-800/90 max-h-[calc(100dvh-2rem)] flex flex-col overflow-hidden"
        >
          <div className="flex items-start justify-between gap-3 pb-3.5 shrink-0">
            <div className="min-w-0">
              <Dialog.Title className="text-lg font-black tracking-tight text-stone-950 dark:text-white">
                Historia
              </Dialog.Title>
              <Dialog.Description className="text-xs text-stone-500 dark:text-stone-400 truncate">
                {chore.title}
              </Dialog.Description>
            </div>
            <Dialog.Close asChild>
              <button
                type="button"
                aria-label="Zamknij"
                className="p-1.5 text-stone-400 hover:text-stone-600 dark:hover:text-stone-200 rounded-lg hover:bg-stone-100 dark:hover:bg-stone-800 transition cursor-pointer"
              >
                <X className="h-4 w-4" />
              </button>
            </Dialog.Close>
          </div>

          <div className="flex-1 overflow-y-auto overscroll-contain min-h-0 touch-pan-y flex flex-col gap-4">
            {isLoading && (
              <div className="flex justify-center py-10 text-stone-400">
                <Loader2 className="h-5 w-5 animate-spin" />
              </div>
            )}

            {isError && (
              <div className="flex items-center gap-2 text-sm text-rose-600 dark:text-rose-400 py-6 justify-center">
                <AlertCircle className="h-4 w-4" />
                Nie udało się pobrać historii.
              </div>
            )}

            {summary && summary.totalCount === 0 && (
              <div className="flex flex-col items-center gap-2 py-10 text-center text-stone-500 dark:text-stone-400">
                <History className="h-6 w-6 text-stone-300 dark:text-stone-600" />
                <p className="text-sm">To zadanie nie zostało jeszcze wykonane.</p>
              </div>
            )}

            {summary && summary.totalCount > 0 && (
              <>
                <HistorySummary summary={summary} />

                <div className="flex flex-col gap-4">
                  {groupByMonth(items).map((group) => (
                    <section key={group.label} className="flex flex-col gap-1.5">
                      <h4
                        data-testid="history-month-header"
                        className="text-[11px] font-semibold uppercase tracking-wider text-stone-500 dark:text-stone-400"
                      >
                        {group.label}
                      </h4>
                      <ul className="flex flex-col divide-y divide-stone-100 dark:divide-stone-800/80">
                        {group.items.map((item) => (
                          <li
                            key={item.id}
                            data-testid="history-row"
                            className="flex items-center justify-between gap-3 py-2"
                          >
                            <div className="min-w-0 flex flex-col">
                              <span className="text-[13px] font-semibold text-stone-900 dark:text-stone-50">
                                {dateTimeFormatter.format(new Date(item.completedAt))}
                                <span className="font-normal text-stone-500 dark:text-stone-400">
                                  {' · '}
                                  {item.memberName}
                                </span>
                              </span>
                              <span
                                data-testid="history-row-gap"
                                className="text-[11px] text-stone-500 dark:text-stone-400"
                              >
                                {formatGap(item.daysSincePrevious, item.id === oldestId)} · +
                                {item.pointsAwarded} pkt
                              </span>
                            </div>
                            {item.urgencyAtCompletion !== 'Unscheduled' && (
                              <span data-testid="history-row-outcome" className="shrink-0">
                                <OutcomeBadge outcome={item.urgencyAtCompletion} />
                              </span>
                            )}
                          </li>
                        ))}
                      </ul>
                    </section>
                  ))}
                </div>

                {hasNextPage && (
                  <button
                    type="button"
                    data-testid="history-load-more"
                    onClick={() => fetchNextPage()}
                    disabled={isFetchingNextPage}
                    className="self-center py-2 px-4 text-xs font-semibold text-amber-800 dark:text-amber-400 hover:bg-amber-50 dark:hover:bg-amber-950/40 rounded-xl transition cursor-pointer disabled:opacity-50"
                  >
                    {isFetchingNextPage ? 'Ładowanie...' : 'Pokaż więcej'}
                  </button>
                )}
              </>
            )}
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}
