export interface BarSegment {
  key: string
  label: string
  value: number
  barClass: string
  dotClass: string
  /** Shown instead of the share computed from `value` (e.g. a server-rounded percentage). */
  percentage?: number
  /** Muted text after the percentage; defaults to the value in parentheses. */
  detail?: string
  testId?: string
}

interface SegmentedBarProps {
  segments: BarSegment[]
  /** `large` is the standalone scoreboard card look; `compact` fits inside dialogs. */
  size?: 'large' | 'compact'
}

const SIZE_STYLES = {
  large: { root: 'gap-3', bar: 'h-4', legend: 'justify-between gap-2 pt-1', item: 'gap-2' },
  compact: { root: 'gap-2', bar: 'h-2.5', legend: 'gap-x-4 gap-y-1', item: 'gap-1.5' },
}

export function SegmentedBar({ segments, size = 'compact' }: SegmentedBarProps) {
  const styles = SIZE_STYLES[size]
  const total = segments.reduce((sum, s) => sum + s.value, 0)
  const shareOf = (s: BarSegment) =>
    s.percentage ?? (total > 0 ? Math.round((s.value / total) * 100) : 0)

  return (
    <div className={`flex flex-col ${styles.root}`}>
      <div className={`${styles.bar} w-full bg-stone-100 dark:bg-stone-800 rounded-full overflow-hidden flex`}>
        {total === 0 ? (
          <div className="w-full h-full bg-stone-200 dark:bg-stone-800" />
        ) : (
          segments
            .filter((s) => s.value > 0)
            .map((s) => (
              <div
                key={s.key}
                style={{ width: `${(s.value / total) * 100}%` }}
                title={`${s.label}: ${shareOf(s)}%`}
                className={`h-full ${s.barClass} transition-all duration-300 first:rounded-l-full last:rounded-r-full`}
              />
            ))
        )}
      </div>

      <div className={`flex items-center flex-wrap ${styles.legend}`}>
        {segments.map((s) => (
          <div key={s.key} data-testid={s.testId} className={`flex items-center text-xs ${styles.item}`}>
            <span className={`w-2.5 h-2.5 rounded-full shrink-0 ${s.dotClass}`} />
            <span className="font-medium text-stone-700 dark:text-stone-300">{s.label}</span>
            <span className="font-bold text-stone-900 dark:text-stone-100">{shareOf(s)}%</span>
            <span className="text-[11px] text-stone-400 dark:text-stone-500">{s.detail ?? `(${s.value})`}</span>
          </div>
        ))}
      </div>
    </div>
  )
}
