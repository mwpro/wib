import { useEffect, useMemo, useState } from 'react'
import {
  CADENCE_PRESETS,
  getCadenceBucket,
  parseCadenceFilter,
  type CadenceFilter,
} from '../utils/cadence'
import type { ChoreResponse } from '../types/chore'

interface BacklogFilters {
  searchQuery: string
  selectedTags: string[]
  cadence: CadenceFilter | null
}

const NO_FILTERS: BacklogFilters = { searchQuery: '', selectedTags: [], cadence: null }

function readFromUrl(): BacklogFilters {
  const params = new URLSearchParams(window.location.search)
  return {
    searchQuery: params.get('q') ?? '',
    selectedTags: params.getAll('tag'),
    cadence: parseCadenceFilter(params.get('cadence')),
  }
}

function toSearchString({ searchQuery, selectedTags, cadence }: BacklogFilters): string {
  const params = new URLSearchParams()
  if (searchQuery) params.set('q', searchQuery)
  selectedTags.forEach((tag) => params.append('tag', tag))
  if (cadence != null) params.set('cadence', String(cadence))
  const search = params.toString()
  return search ? `?${search}` : ''
}

function deriveBacklogView(chores: ChoreResponse[], filters: BacklogFilters) {
  const availableTags = Array.from(new Set(chores.flatMap((c) => c.tags ?? []))).sort()
  const selectedTags = filters.selectedTags.filter((t) => availableTags.includes(t))
  const { cadence } = filters

  const presentCadences = new Set<CadenceFilter>(chores.map((c) => getCadenceBucket(c.cadenceDays)))
  if (cadence != null) presentCadences.add(cadence)
  const availableCadences = [...CADENCE_PRESETS.map((p) => p.days), 'none' as const].filter((c) =>
    presentCadences.has(c)
  )

  const q = filters.searchQuery.trim().toLowerCase()
  const visibleChores = chores.filter((chore) => {
    const matchesText =
      !q ||
      chore.title.toLowerCase().includes(q) ||
      chore.description?.toLowerCase().includes(q)
    if (!matchesText) return false
    if (selectedTags.length > 0 && !selectedTags.some((t) => (chore.tags ?? []).includes(t))) {
      return false
    }
    return cadence == null || getCadenceBucket(chore.cadenceDays) === cadence
  })

  return {
    visibleChores,
    availableTags,
    selectedTags,
    availableCadences,
    activeFiltersCount:
      (filters.searchQuery ? 1 : 0) + selectedTags.length + (cadence != null ? 1 : 0),
  }
}

export function useBacklogView(chores: ChoreResponse[]) {
  const [filters, setFilters] = useState<BacklogFilters>(readFromUrl)

  useEffect(() => {
    const search = toSearchString(filters)
    if (window.location.search !== search) {
      window.history.replaceState(null, '', `${window.location.pathname}${search}`)
    }
  }, [filters])

  const view = useMemo(() => deriveBacklogView(chores, filters), [chores, filters])

  return {
    ...view,
    searchQuery: filters.searchQuery,
    cadence: filters.cadence,
    setSearchQuery: (searchQuery: string) => setFilters((f) => ({ ...f, searchQuery })),
    setCadence: (cadence: CadenceFilter | null) => setFilters((f) => ({ ...f, cadence })),
    toggleTag: (tag: string) =>
      setFilters((f) => ({
        ...f,
        selectedTags: view.selectedTags.includes(tag)
          ? view.selectedTags.filter((t) => t !== tag)
          : [...view.selectedTags, tag],
      })),
    clearTags: () => setFilters((f) => ({ ...f, selectedTags: [] })),
    clearAll: () => setFilters(NO_FILTERS),
  }
}
