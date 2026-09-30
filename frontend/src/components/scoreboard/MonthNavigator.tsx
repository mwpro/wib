import { ChevronLeft, ChevronRight } from 'lucide-react'

interface MonthNavigatorProps {
  year: number
  month: number
  onChange: (year: number, month: number) => void
}

const MONTH_NAMES = [
  'Styczeń',
  'Luty',
  'Marzec',
  'Kwiecień',
  'Maj',
  'Czerwiec',
  'Lipiec',
  'Sierpień',
  'Wrzesień',
  'Październik',
  'Listopad',
  'Grudzień',
]

export function MonthNavigator({ year, month, onChange }: MonthNavigatorProps) {
  const now = new Date()
  const currentYear = now.getFullYear()
  const currentMonth = now.getMonth() + 1 // 1-indexed

  const isCurrentOrFuture = year > currentYear || (year === currentYear && month >= currentMonth)

  const handlePrev = () => {
    if (month === 1) {
      onChange(year - 1, 12)
    } else {
      onChange(year, month - 1)
    }
  }

  const handleNext = () => {
    if (isCurrentOrFuture) return
    if (month === 12) {
      onChange(year + 1, 1)
    } else {
      onChange(year, month + 1)
    }
  }

  const monthLabel = MONTH_NAMES[month - 1] ?? ''

  return (
    <div
      data-testid="month-navigator"
      className="flex items-center justify-between bg-white dark:bg-[#14161d] border border-stone-200/90 dark:border-stone-800/80 rounded-2xl px-4 py-2.5 shadow-xs"
    >
      <button
        type="button"
        onClick={handlePrev}
        aria-label="Poprzedni miesiąc"
        className="p-1.5 rounded-xl hover:bg-stone-100 dark:hover:bg-stone-800 text-stone-600 dark:text-stone-300 transition cursor-pointer"
      >
        <ChevronLeft className="h-5 w-5" />
      </button>

      <div className="text-center">
        <span className="text-sm font-bold text-stone-900 dark:text-stone-100 tracking-tight">
          {monthLabel} {year}
        </span>
      </div>

      <button
        type="button"
        onClick={handleNext}
        disabled={isCurrentOrFuture}
        aria-label="Następny miesiąc"
        className="p-1.5 rounded-xl hover:bg-stone-100 dark:hover:bg-stone-800 text-stone-600 dark:text-stone-300 transition cursor-pointer disabled:opacity-30 disabled:cursor-not-allowed"
      >
        <ChevronRight className="h-5 w-5" />
      </button>
    </div>
  )
}
