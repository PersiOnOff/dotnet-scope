export type EventType = 'http' | 'sql' | 'log' | 'exception'

export type TelemetryEvent = {
  id: string
  requestId?: string | null
  timestamp: string
  type: EventType
  method?: string | null
  path?: string | null
  statusCode?: number | null
  durationMs?: number | null
  requestBody?: string | null
  responseBody?: string | null
  sql?: string | null
  parameters?: string | null
  logLevel?: string | null
  category?: string | null
  message?: string | null
  exception?: string | null
}

export type SortMode = 'newest' | 'slowest'

export type FilterState = {
  type: EventType | 'all'
  search: string
  minDurationMs: number
  sort: SortMode
}

export type SavedFilter = FilterState & {
  id: string
  name: string
}
