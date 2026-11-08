import type { FilterState, SavedFilter, SortMode, TelemetryEvent } from '../types'
import { summarize } from './format'

const STORAGE_KEY = 'dotnetscope.filters'

/** Filters are a personal preference, so they live in the browser, not in the dashboard. */
export function loadSavedFilters(): SavedFilter[] {
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY)
    if (!raw) return []
    const parsed: unknown = JSON.parse(raw)
    if (!Array.isArray(parsed)) return []
    return parsed.filter(isSavedFilter)
  } catch {
    return []
  }
}

export function persistSavedFilters(filters: SavedFilter[]): void {
  try {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(filters))
  } catch {
    // Private mode or a full quota: the filters are a convenience, not data worth failing over.
  }
}

export function matchesFilter(event: TelemetryEvent, filter: Pick<FilterState, 'type' | 'search' | 'minDurationMs'>): boolean {
  if (filter.type !== 'all' && event.type !== filter.type) return false
  if (filter.minDurationMs > 0 && (event.durationMs ?? 0) < filter.minDurationMs) return false

  const term = filter.search.trim().toLowerCase()
  if (!term) return true

  return (
    summarize(event).toLowerCase().includes(term) ||
    (event.path ?? '').toLowerCase().includes(term) ||
    (event.sql ?? '').toLowerCase().includes(term) ||
    (event.parameters ?? '').toLowerCase().includes(term) ||
    (event.message ?? '').toLowerCase().includes(term) ||
    (event.category ?? '').toLowerCase().includes(term) ||
    (event.responseBody ?? '').toLowerCase().includes(term) ||
    (event.traceId ?? '').toLowerCase().includes(term)
  )
}

export function sortEvents(events: TelemetryEvent[], mode: SortMode): TelemetryEvent[] {
  if (mode === 'newest') return events

  // Events that carry no duration (logs) belong at the bottom, not at the top with a fake 0.
  return [...events].sort((left, right) => {
    const leftDuration = left.durationMs ?? -1
    const rightDuration = right.durationMs ?? -1
    return rightDuration - leftDuration
  })
}

function isSavedFilter(value: unknown): value is SavedFilter {
  if (typeof value !== 'object' || value === null) return false
  const candidate = value as Record<string, unknown>
  return (
    typeof candidate.id === 'string' &&
    typeof candidate.name === 'string' &&
    typeof candidate.type === 'string' &&
    typeof candidate.search === 'string' &&
    typeof candidate.minDurationMs === 'number' &&
    (candidate.sort === 'newest' || candidate.sort === 'slowest')
  )
}
