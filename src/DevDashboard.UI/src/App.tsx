import { useEffect, useMemo, useState } from 'react'
import Sidebar from './components/Sidebar'
import Toolbar from './components/Toolbar'
import EventList from './components/EventList'
import EventDetails from './components/EventDetails'
import { useTelemetry } from './hooks/useTelemetry'
import type { EventType, TelemetryEvent } from './types'
import { summarize } from './lib/format'

type Tab = EventType | 'all'

export default function App() {
  const { events, status, paused, setPaused, clear } = useTelemetry()
  const [tab, setTab] = useState<Tab>('all')
  const [search, setSearch] = useState('')
  const [minDurationMs, setMinDurationMs] = useState(0)
  const [selectedId, setSelectedId] = useState<string | null>(null)

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

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase()
    return events.filter(event => {
      if (tab !== 'all' && event.type !== tab) return false
      if (minDurationMs > 0 && (event.durationMs ?? 0) < minDurationMs) return false
      if (!term) return true
      return (
        summarize(event).toLowerCase().includes(term) ||
        (event.path ?? '').toLowerCase().includes(term) ||
        (event.sql ?? '').toLowerCase().includes(term) ||
        (event.parameters ?? '').toLowerCase().includes(term) ||
        (event.message ?? '').toLowerCase().includes(term) ||
        (event.category ?? '').toLowerCase().includes(term) ||
        (event.responseBody ?? '').toLowerCase().includes(term)
      )
    })
  }, [events, tab, search, minDurationMs])

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

  return (
    <div className="flex h-full bg-slate-950 text-slate-200">
      <Sidebar
        active={tab}
        counts={counts}
        status={status}
        paused={paused}
        onChange={setTab}
      />

      <main className="flex min-w-0 flex-1 flex-col">
        <Toolbar
          search={search}
          minDurationMs={minDurationMs}
          paused={paused}
          visible={filtered.length}
          total={events.length}
          onSearch={setSearch}
          onMinDuration={setMinDurationMs}
          onTogglePause={() => setPaused(!paused)}
          onClear={() => {
            setSelectedId(null)
            void clear()
          }}
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
      </main>
    </div>
  )
}
