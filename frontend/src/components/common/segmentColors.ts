export const SEGMENT_COLORS = [
  {
    bg: 'bg-emerald-500 dark:bg-emerald-400',
    indicator: 'bg-emerald-500',
    text: 'text-emerald-700 dark:text-emerald-400',
  },
  {
    bg: 'bg-amber-500 dark:bg-amber-400',
    indicator: 'bg-amber-500',
    text: 'text-amber-700 dark:text-amber-400',
  },
  {
    bg: 'bg-sky-500 dark:bg-sky-400',
    indicator: 'bg-sky-500',
    text: 'text-sky-700 dark:text-sky-400',
  },
  {
    bg: 'bg-purple-500 dark:bg-purple-400',
    indicator: 'bg-purple-500',
    text: 'text-purple-700 dark:text-purple-400',
  },
]

export function segmentColor(index: number) {
  return SEGMENT_COLORS[index % SEGMENT_COLORS.length]
}
