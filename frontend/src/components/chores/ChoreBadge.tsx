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
      'bg-emerald-100 text-emerald-800 dark:bg-emerald-950/80 dark:text-emerald-300 border-emerald-200 dark:border-emerald-800',
  },
  DueSoon: {
    label: 'Wkrótce',
    className:
      'bg-amber-100 text-amber-800 dark:bg-amber-950/80 dark:text-amber-300 border-amber-200 dark:border-amber-800',
  },
  Overdue: {
    label: 'Zaległe',
    className:
      'bg-orange-100 text-orange-800 dark:bg-orange-950/80 dark:text-orange-300 border-orange-200 dark:border-orange-800',
  },
  Neglected: {
    label: 'Zaniedbane',
    className:
      'bg-rose-100 text-rose-800 dark:bg-rose-950/80 dark:text-rose-300 border-rose-200 dark:border-rose-800 animate-pulse',
  },
  Unscheduled: {
    label: 'Bez terminu',
    className:
      'bg-slate-100 text-slate-700 dark:bg-slate-800/80 dark:text-slate-300 border-slate-200 dark:border-slate-700',
  },
}

export function ChoreBadge({ urgency }: ChoreBadgeProps) {
  const config = BADGE_CONFIG[urgency] || BADGE_CONFIG.Unscheduled

  return (
    <span
      className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold border ${config.className}`}
    >
      {config.label}
    </span>
  )
}
