import type { FreshnessUrgency } from '../../types/chore'

interface ChoreBadgeProps {
  urgency: FreshnessUrgency
}

const BADGE_CONFIG: Record<
  FreshnessUrgency,
  { label: string; className: string }
> = {
  Fresh: {
    label: 'Świeże',
    className:
      'bg-emerald-100 text-emerald-900 dark:bg-emerald-950/70 dark:text-emerald-300 border-emerald-300 dark:border-emerald-800',
  },
  DueSoon: {
    label: 'Wkrótce',
    className:
      'bg-amber-100 text-amber-950 dark:bg-amber-950/70 dark:text-amber-200 border-amber-300 dark:border-amber-800',
  },
  Overdue: {
    label: 'Zaległe',
    className:
      'bg-orange-100 text-orange-950 dark:bg-orange-950/70 dark:text-orange-200 border-orange-300 dark:border-orange-800',
  },
  Neglected: {
    label: 'Zaniedbane',
    className:
      'bg-rose-100 text-rose-950 dark:bg-rose-950/70 dark:text-rose-200 border-rose-300 dark:border-rose-800',
  },
  Unscheduled: {
    label: 'Bez terminu',
    className:
      'bg-stone-100 text-stone-700 dark:bg-stone-800 dark:text-stone-300 border-stone-300 dark:border-stone-700',
  },
}

export function ChoreBadge({ urgency }: ChoreBadgeProps) {
  const config = BADGE_CONFIG[urgency] || BADGE_CONFIG.Unscheduled

  return (
    <span
      className={`inline-flex items-center px-2 py-0.5 rounded-md text-xs font-semibold border ${config.className}`}
    >
      {config.label}
    </span>
  )
}
