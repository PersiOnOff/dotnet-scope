type Props = {
  search: string
  minDurationMs: number
  paused: boolean
  visible: number
  total: number
  onSearch: (value: string) => void
  onMinDuration: (value: number) => void
  onTogglePause: () => void
  onClear: () => void
}

const DURATIONS = [
  { value: 0, label: 'Any duration' },
  { value: 100, label: '> 100 ms' },
  { value: 500, label: '> 500 ms' },
  { value: 1000, label: '> 1 s' },
]

export default function Toolbar({
  search,
  minDurationMs,
  paused,
  visible,
  total,
  onSearch,
  onMinDuration,
  onTogglePause,
  onClear,
}: Props) {
  return (
    <div className="flex flex-wrap items-center gap-2 border-b border-slate-800 bg-slate-900/40 px-4 py-2.5">
      <div className="relative min-w-[12rem] flex-1">
        <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 font-mono text-xs text-slate-500">/</span>
        <input
          value={search}
          onChange={event => onSearch(event.target.value)}
          placeholder="Filter by path, SQL, message…"
          className="w-full rounded-lg border border-slate-800 bg-slate-950/80 py-1.5 pl-8 pr-3 text-sm text-slate-200 placeholder:text-slate-600 focus:border-indigo-500/60 focus:outline-none focus:ring-1 focus:ring-indigo-500/40"
        />
      </div>

      <select
        value={minDurationMs}
        onChange={event => onMinDuration(Number(event.target.value))}
        className="rounded-lg border border-slate-800 bg-slate-950/80 px-2.5 py-1.5 text-sm text-slate-300 focus:border-indigo-500/60 focus:outline-none"
      >
        {DURATIONS.map(option => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>

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
        {visible === total ? `${total} events` : `${visible} / ${total}`}
      </span>
    </div>
  )
}
