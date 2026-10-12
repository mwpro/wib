export const CADENCE_PRESETS = [
  { label: 'Codziennie', days: 1 },
  { label: 'Co tydzień', days: 7 },
  { label: 'Co miesiąc', days: 30 },
  { label: 'Co 3 mies.', days: 90 },
  { label: 'Co pół roku', days: 180 },
  { label: 'Co rok', days: 365 },
] as const

/** Upper edge (in days) of a cadence preset bucket, or 'none' for unscheduled chores. */
export type CadenceFilter = number | 'none'

/**
 * Maps a chore's cadence to the preset bucket it belongs to: each preset covers the days
 * above the previous preset up to and including its own value; the last preset is open-ended.
 */
export function getCadenceBucket(cadenceDays: number | null | undefined): CadenceFilter {
  if (cadenceDays == null || cadenceDays <= 0) return 'none'
  const preset = CADENCE_PRESETS.find((p) => cadenceDays <= p.days)
  return preset ? preset.days : CADENCE_PRESETS[CADENCE_PRESETS.length - 1].days
}

export function parseCadenceFilter(value: string | null): CadenceFilter | null {
  if (value === 'none') return 'none'
  const days = Number(value)
  return CADENCE_PRESETS.some((p) => p.days === days) ? days : null
}
