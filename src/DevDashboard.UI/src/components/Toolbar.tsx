import { useState, type FormEvent } from 'react'
import type { SavedFilter, SortMode } from '../types'
import { formatCount } from '../lib/format'

type Props = {
  search: string
  minDurationMs: number
  sort: SortMode
  paused: boolean
  visible: number
  total: number
  savedFilters: SavedFilter[]
  onSearch: (value: string) => void
  onMinDuration: (value: number) => void
  onSort: (value: SortMode) => void
  onTogglePause: () => void
  onClear: () => void
  onSaveFilter: (name: string) => void
  onApplyFilter: (filter: SavedFilter) => void
  onDeleteFilter: (id: string) => void
}

const DURATIONS = [
  { value: 0, label: 'Any duration' },
  { value: 100, label: '> 100 ms' },
  { value: 500, label: '> 500 ms' },
  { value: 1000, label: '> 1 s' },
]

const CONTROL =
  'rounded-lg border border-slate-800 bg-slate-950/80 px-2.5 py-1.5 text-sm text-slate-300 focus:border-indigo-500/60 focus:outline-none'

export default function Toolbar({
  search,
  minDurationMs,
  sort,
  paused,
  visible,
  total,
  savedFilters,
  onSearch,
  onMinDuration,
  onSort,
  onTogglePause,
  onClear,
  onSaveFilter,
  onApplyFilter,
  onDeleteFilter,
}: Props) {
  const [naming, setNaming] = useState(false)
  const [name, setName] = useState('')

  const submit = (event: FormEvent) => {
    event.preventDefault()
    const trimmed = name.trim()
    if (!trimmed) return
    onSaveFilter(trimmed)
    setName('')
    setNaming(false)
  }

  return (
    <div className="border-b border-slate-800 bg-slate-900/40">
      <div className="flex flex-wrap items-center gap-2 px-4 py-2.5">
        <div className="relative min-w-[12rem] flex-1">
          <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 font-mono text-xs text-slate-500">/</span>
          <input
            value={search}
            onChange={event => onSearch(event.target.value)}
            placeholder="Filter by path, SQL, message…"
            className="w-full rounded-lg border border-slate-800 bg-slate-950/80 py-1.5 pl-8 pr-3 text-sm text-slate-200 placeholder:text-slate-600 focus:border-indigo-500/60 focus:outline-none focus:ring-1 focus:ring-indigo-500/40"
          />
        </div>

        <select value={minDurationMs} onChange={event => onMinDuration(Number(event.target.value))} className={CONTROL}>
          {DURATIONS.map(option => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>

        <select
          value={sort}
          onChange={event => onSort(event.target.value === 'slowest' ? 'slowest' : 'newest')}
          className={CONTROL}
          title="Sort order"
          aria-label="Sort order"
        >
          <option value="newest">Newest first</option>
          <option value="slowest">Slowest first</option>
        </select>

        {naming ? (
          <form onSubmit={submit} className="flex items-center gap-1">
            <input
              autoFocus
              value={name}
              onChange={event => setName(event.target.value)}
              onKeyDown={event => {
                if (event.key === 'Escape') {
                  setName('')
                  setNaming(false)
                }
              }}
              placeholder="Name this filter"
              className="w-36 rounded-lg border border-indigo-500/40 bg-slate-950 px-2.5 py-1.5 text-sm text-slate-200 placeholder:text-slate-600 focus:outline-none"
            />
            <button type="submit" className="rounded-lg border border-indigo-500/40 bg-indigo-500/10 px-2.5 py-1.5 text-sm text-indigo-300 hover:bg-indigo-500/20">
              Save
            </button>
          </form>
        ) : (
          <button
            onClick={() => setNaming(true)}
            className="rounded-lg border border-slate-800 bg-slate-950/80 px-3 py-1.5 text-sm text-slate-300 transition hover:border-indigo-500/40 hover:text-indigo-300"
          >
            ☆ Save filter
          </button>
        )}

        <button
          onClick={onTogglePause}
          className={`rounded-lg border px-3 py-1.5 text-sm transition ${
            paused
              ? 'border-amber-500/40 bg-amber-500/10 text-amber-300 hover:bg-amber-500/20'
              : 'border-slate-800 bg-slate-950/80 text-slate-300 hover:bg-slate-800'
          }`}
        >
          {paused ? '▶ Resume' : '❙❙ Pause'}
        </button>

        <button
          onClick={onClear}
          className="rounded-lg border border-slate-800 bg-slate-950/80 px-3 py-1.5 text-sm text-slate-300 transition hover:border-rose-500/40 hover:bg-rose-500/10 hover:text-rose-300"
        >
          Clear
        </button>

        <span className="ml-1 font-mono text-[11px] text-slate-600">
          {visible === total ? formatCount(total, 'event', 'events') : `${visible} / ${total}`}
        </span>
      </div>

      {savedFilters.length > 0 && (
        <div className="flex flex-wrap items-center gap-1.5 px-4 pb-2.5">
          <span className="text-[11px] uppercase tracking-wider text-slate-600">Saved</span>
          {savedFilters.map(filter => (
            <span
              key={filter.id}
              className="inline-flex items-center gap-1 rounded-full border border-slate-800 bg-slate-950/80 py-0.5 pl-2.5 pr-1 text-[11px] text-slate-400"
            >
              <button onClick={() => onApplyFilter(filter)} className="hover:text-indigo-300" title="Apply this filter">
                {filter.name}
              </button>
              <button
                onClick={() => onDeleteFilter(filter.id)}
                className="rounded-full px-1 text-slate-600 hover:bg-slate-800 hover:text-rose-300"
                title="Delete this filter"
              >
                ×
              </button>
            </span>
          ))}
        </div>
      )}
    </div>
  )
}
