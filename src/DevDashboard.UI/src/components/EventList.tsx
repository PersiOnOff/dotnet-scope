import type { TelemetryEvent } from '../types'
import { TYPE_STYLE, durationTone, formatDuration, formatTime, statusTone, summarize } from '../lib/format'

type Props = {
  events: TelemetryEvent[]
  total: number
  hasFilter: boolean
  selectedId: string | null
  childCount: Record<string, number>
  onSelect: (event: TelemetryEvent) => void
}

const ROW = 'grid grid-cols-[7.5rem_4rem_1fr_3.5rem_5rem] items-center gap-3 px-4'

export default function EventList({ events, total, hasFilter, selectedId, childCount, onSelect }: Props) {
  if (events.length === 0) {
    return (
      <div className="flex flex-1 items-center justify-center p-8">
        {total === 0 ? <Onboarding /> : <NoMatches onResetHint={hasFilter} />}
      </div>
    )
  }

  return (
    <div className="flex min-w-0 flex-1 flex-col">
      <div className={`${ROW} border-b border-slate-800 bg-slate-950/60 py-2 text-[11px] uppercase tracking-wider text-slate-500`}>
        <span>Time</span>
        <span>Type</span>
        <span>Summary</span>
        <span>Status</span>
        <span className="text-right">Duration</span>
      </div>

      <div className="flex-1 overflow-y-auto">
        {events.map(event => {
          const style = TYPE_STYLE[event.type]
          const summary = summarize(event)
          const children = event.type === 'http' ? childCount[event.id] ?? 0 : 0
          const isSelected = selectedId === event.id

          return (
            <button
              key={event.id}
              onClick={() => onSelect(event)}
              title={summary}
              className={`${ROW} w-full border-b border-slate-900/80 py-2 text-left text-sm transition ${
                isSelected ? 'bg-indigo-500/10' : 'hover:bg-slate-900/70'
              }`}
            >
              <span className="font-mono text-[11px] text-slate-500">{formatTime(event.timestamp)}</span>

              <span className={`w-fit rounded px-1.5 py-0.5 font-mono text-[10px] font-semibold ring-1 ring-inset ${style.badge}`}>
                {style.label}
              </span>

              <span className="flex min-w-0 items-center gap-2">
                <span className={`truncate font-mono text-[13px] ${event.type === 'exception' ? 'text-rose-300' : 'text-slate-300'}`}>
                  {summary || '—'}
                </span>
                {children > 0 && (
                  <span className="shrink-0 rounded bg-slate-800 px-1.5 py-0.5 font-mono text-[10px] text-slate-400">
                    +{children}
                  </span>
                )}
              </span>

              <span className={`font-mono text-xs ${statusTone(event.statusCode)}`}>{event.statusCode ?? ''}</span>

              <span className={`text-right font-mono text-xs ${durationTone(event.durationMs)}`}>
                {formatDuration(event.durationMs)}
              </span>
            </button>
          )
        })}
      </div>
    </div>
  )
}

function Onboarding() {
  return (
    <div className="max-w-lg space-y-4 text-center">
      <div className="mx-auto grid h-12 w-12 place-items-center rounded-xl bg-slate-900 text-xl text-indigo-400 ring-1 ring-inset ring-slate-800">
        ◍
      </div>
      <div>
        <h2 className="text-base font-semibold text-slate-200">Nothing captured yet</h2>
        <p className="mt-1 text-sm text-slate-500">Hit your API and the request, its SQL and its logs appear here in real time.</p>
      </div>
      <div className="space-y-1.5 rounded-xl border border-slate-800 bg-slate-950/80 p-4 text-left font-mono text-[12px] text-slate-400">
        <div className="text-slate-600"># in another terminal</div>
        <div>curl http://localhost:5000/api/todos</div>
        <div>curl -X POST "http://localhost:5000/api/todos?title=hello"</div>
        <div>curl http://localhost:5000/api/error</div>
      </div>
    </div>
  )
}

function NoMatches({ onResetHint }: { onResetHint: boolean }) {
  return (
    <div className="text-center text-sm text-slate-500">
      No event matches this filter.
      {onResetHint && <div className="mt-1 text-xs text-slate-600">Clear the search box or widen the duration filter.</div>}
    </div>
  )
}
