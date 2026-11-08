import type { EventType } from '../types'
import type { ConnectionStatus } from '../hooks/useTelemetry'
import { useI18n, type Locale } from '../i18n'

type Tab = EventType | 'all'

const ICONS: Record<Tab, string> = {
  all: '◍',
  http: '⇄',
  sql: '⌗',
  log: '≡',
  exception: '!',
}

const STATUS_TONE: Record<ConnectionStatus, string> = {
  connecting: 'bg-amber-400',
  connected: 'bg-emerald-400',
  reconnecting: 'bg-amber-400',
  offline: 'bg-rose-500',
}

type Props = {
  active: Tab
  view: 'events' | 'database'
  hasDatabase: boolean
  counts: Record<string, number>
  status: ConnectionStatus
  paused: boolean
  onChange: (id: Tab) => void
  onView: (view: 'events' | 'database') => void
}

export default function Sidebar({ active, view, hasDatabase, counts, status, paused, onChange, onView }: Props) {
  const { t, locale, setLocale } = useI18n()

  const tabs: { id: Tab; label: string }[] = [
    { id: 'all', label: t.nav.all },
    { id: 'http', label: t.nav.http },
    { id: 'sql', label: t.nav.sql },
    { id: 'log', label: t.nav.log },
    { id: 'exception', label: t.nav.exception },
  ]

  return (
    <aside className="flex w-60 shrink-0 flex-col border-r border-slate-800 bg-slate-950/70">
      <div className="flex items-center gap-2.5 px-5 py-5">
        <span className="grid h-8 w-8 place-items-center rounded-lg bg-indigo-500/15 font-mono text-sm font-bold text-indigo-300 ring-1 ring-inset ring-indigo-500/40">
          ⠿
        </span>
        <div>
          <div className="text-sm font-semibold tracking-tight text-slate-100">{t.app.name}</div>
          <div className="text-[11px] text-slate-500">{t.app.tagline}</div>
        </div>
      </div>

      <nav className="flex flex-col gap-0.5 px-3">
        {tabs.map(tab => {
          const isActive = view === 'events' && active === tab.id
          return (
            <button
              key={tab.id}
              onClick={() => onChange(tab.id)}
              className={`group flex items-center gap-3 rounded-lg px-3 py-2 text-left text-sm transition ${
                isActive
                  ? 'bg-slate-800/80 text-slate-100'
                  : 'text-slate-400 hover:bg-slate-900 hover:text-slate-200'
              }`}
            >
              <span className={`w-4 text-center font-mono ${isActive ? 'text-indigo-300' : 'text-slate-600'}`}>{ICONS[tab.id]}</span>
              <span className="flex-1">{tab.label}</span>
              <span className="rounded-md bg-slate-900 px-1.5 py-0.5 font-mono text-[11px] text-slate-500 ring-1 ring-inset ring-slate-800">
                {counts[tab.id] ?? 0}
              </span>
            </button>
          )
        })}

        {hasDatabase && (
          <>
            <div className="my-2 border-t border-slate-800" />
            <button
              onClick={() => onView('database')}
              className={`group flex items-center gap-3 rounded-lg px-3 py-2 text-left text-sm transition ${
                view === 'database'
                  ? 'bg-slate-800/80 text-slate-100'
                  : 'text-slate-400 hover:bg-slate-900 hover:text-slate-200'
              }`}
            >
              <span className={`w-4 text-center font-mono ${view === 'database' ? 'text-indigo-300' : 'text-slate-600'}`}>⌸</span>
              <span className="flex-1">{t.nav.database}</span>
            </button>
          </>
        )}
      </nav>

      <div className="mt-auto space-y-3 border-t border-slate-800 px-5 py-4">
        <div className="flex items-center justify-between text-[11px] text-slate-600">
          <span>{t.language.label}</span>
          <div className="flex items-center gap-0.5 rounded-md border border-slate-800 p-0.5">
            {(['en', 'ru'] as Locale[]).map(code => (
              <button
                key={code}
                onClick={() => setLocale(code)}
                aria-label={t.language.label}
                className={`rounded px-1.5 py-0.5 uppercase transition ${
                  locale === code ? 'bg-slate-800 text-slate-200' : 'text-slate-600 hover:text-slate-400'
                }`}
              >
                {code}
              </button>
            ))}
          </div>
        </div>

        <div className="flex items-center gap-2 text-xs text-slate-500">
          <span className={`h-1.5 w-1.5 rounded-full ${STATUS_TONE[paused ? 'offline' : status]}`} />
          {paused ? t.connection.paused : t.connection[status === 'connected' ? 'live' : status]}
        </div>

        <p className="text-[11px] leading-relaxed text-slate-600">{t.privacy}</p>
      </div>
    </aside>
  )
}
