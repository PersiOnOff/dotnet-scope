import { useCallback, useEffect, useMemo, useState } from 'react'
import Sidebar from './components/Sidebar'
import Toolbar from './components/Toolbar'
import EventList from './components/EventList'
import EventDetails from './components/EventDetails'
import DatabaseView from './components/DatabaseView'
import { useTelemetry } from './hooks/useTelemetry'
import { loadSavedFilters, matchesFilter, persistSavedFilters, sortEvents } from './lib/filters'
import { API } from './lib/format'
import type { DataSourceInfo, EventType, SavedFilter, SortMode, TelemetryEvent } from './types'

type Tab = EventType | 'all'
type View = 'events' | 'database'

export default function App() {
  const { events, status, paused, setPaused, clear } = useTelemetry()
  const [view, setView] = useState<View>('events')
  const [tab, setTab] = useState<Tab>('all')
  const [search, setSearch] = useState('')
  const [minDurationMs, setMinDurationMs] = useState(0)
  const [sort, setSort] = useState<SortMode>('newest')
  const [savedFilters, setSavedFilters] = useState<SavedFilter[]>(loadSavedFilters)
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [sources, setSources] = useState<DataSourceInfo[]>([])
  const [sourcesLoaded, setSourcesLoaded] = useState(false)

  const loadSources = useCallback(async () => {
    try {
      const response = await fetch(`${API}/db`)
      setSources(response.ok ? ((await response.json()) as DataSourceInfo[]) : [])
    } catch {
      setSources([])
    } finally {
      setSourcesLoaded(true)
    }
  }, [])

  useEffect(() => {
    void loadSources()
  }, [loadSources])

  const counts = useMemo(() => {
    const result: Record<string, number> = { all: events.length }
    for (const event of events) result[event.type] = (result[event.type] ?? 0) + 1
    return result
  }, [events])

  const childCount = useMemo(() => {
    const result: Record<string, number> = {}
    for (const event of events) {
      if (event.requestId && event.type !== 'http') {
        result[event.requestId] = (result[event.requestId] ?? 0) + 1
      }
    }
    return result
  }, [events])

  const filtered = useMemo(
    () => sortEvents(events.filter(event => matchesFilter(event, { type: tab, search, minDurationMs })), sort),
    [events, tab, search, minDurationMs, sort],
  )

  const selected = useMemo<TelemetryEvent | null>(
    () => events.find(event => event.id === selectedId) ?? null,
    [events, selectedId],
  )

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setSelectedId(null)
    }
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [])

  const saveFilter = (name: string) => {
    const filter: SavedFilter = {
      id: crypto.randomUUID(),
      name,
      type: tab,
      search,
      minDurationMs,
      sort,
    }
    setSavedFilters(previous => {
      const next = [...previous.filter(candidate => candidate.name !== name), filter]
      persistSavedFilters(next)
      return next
    })
  }

  const applyFilter = (filter: SavedFilter) => {
    setTab(filter.type)
    setSearch(filter.search)
    setMinDurationMs(filter.minDurationMs)
    setSort(filter.sort)
    setView('events')
  }

  const deleteFilter = (id: string) => {
    setSavedFilters(previous => {
      const next = previous.filter(candidate => candidate.id !== id)
      persistSavedFilters(next)
      return next
    })
  }

  return (
    <div className="flex h-full bg-slate-950 text-slate-200">
      <Sidebar
        active={tab}
        view={view}
        hasDatabase={sources.length > 0}
        counts={counts}
        status={status}
        paused={paused}
        onChange={id => {
          setTab(id)
          setView('events')
        }}
        onView={setView}
      />

      <main className="flex min-w-0 flex-1 flex-col">
        {view === 'database' ? (
          <DatabaseView sources={sources} loaded={sourcesLoaded} onRefresh={() => void loadSources()} />
        ) : (
          <>
            <Toolbar
              search={search}
              minDurationMs={minDurationMs}
              sort={sort}
              paused={paused}
              visible={filtered.length}
              total={events.length}
              savedFilters={savedFilters}
              onSearch={setSearch}
              onMinDuration={setMinDurationMs}
              onSort={setSort}
              onTogglePause={() => setPaused(!paused)}
              onClear={() => {
                setSelectedId(null)
                void clear()
              }}
              onSaveFilter={saveFilter}
              onApplyFilter={applyFilter}
              onDeleteFilter={deleteFilter}
            />

            <div className="flex min-h-0 flex-1">
              <EventList
                events={filtered}
                total={events.length}
                hasFilter={search.length > 0 || minDurationMs > 0 || tab !== 'all'}
                selectedId={selectedId}
                childCount={childCount}
                onSelect={event => setSelectedId(event.id)}
              />
              <EventDetails
                event={selected}
                all={events}
                onSelect={event => setSelectedId(event.id)}
                onClose={() => setSelectedId(null)}
              />
            </div>
          </>
        )}
      </main>
    </div>
  )
}
