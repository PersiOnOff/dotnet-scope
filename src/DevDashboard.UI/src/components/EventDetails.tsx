import { useMemo, useState, type ReactNode } from 'react'
import type { TelemetryEvent } from '../types'
import { toJsonExport, toMarkdown } from '../lib/export'
import {
  TYPE_STYLE,
  durationTone,
  formatDuration,
  formatTime,
  highlightSql,
  prettyJson,
  stackFrames,
  statusTone,
  summarize,
} from '../lib/format'
import { useI18n } from '../i18n'

type Props = {
  event: TelemetryEvent | null
  all: TelemetryEvent[]
  onSelect: (event: TelemetryEvent) => void
  onClose: () => void
}

export default function EventDetails({ event, all, onSelect, onClose }: Props) {
  const { t, locale } = useI18n()

  const children = useMemo(
    () => (event ? all.filter(candidate => candidate.requestId === event.id && candidate.id !== event.id) : []),
    [event, all],
  )

  const parent = useMemo(
    () => (event?.requestId && event.type !== 'http' ? all.find(candidate => candidate.id === event.requestId) ?? null : null),
    [event, all],
  )

  // Exporting a SQL or log event sends the whole request it belongs to: a lone SQL statement
  // without its request is rarely what you want to paste into an issue.
  const bundle = useMemo(() => {
    if (!event) return null
    const request = event.type === 'http' ? event : parent ?? event
    const related = all.filter(candidate => candidate.requestId === request.id && candidate.id !== request.id)
    return { request, related }
  }, [event, parent, all])

  if (!event) {
    return (
      <aside className="flex w-[26rem] shrink-0 flex-col items-center justify-center border-l border-slate-800 bg-slate-950/40 p-8 text-center">
        <div className="grid h-10 w-10 place-items-center rounded-xl bg-slate-900 text-slate-600 ring-1 ring-inset ring-slate-800">
          ↖
        </div>
        <p className="mt-3 text-sm text-slate-500">{t.details.placeholderTitle}</p>
        <p className="mt-1 text-xs text-slate-600">{t.details.placeholderBody}</p>
      </aside>
    )
  }

  const style = TYPE_STYLE[event.type]
  const json = prettyJson(event.responseBody)

  return (
    <aside className="flex w-[26rem] shrink-0 flex-col border-l border-slate-800 bg-slate-950/40">
      <header className="flex items-start gap-3 border-b border-slate-800 px-4 py-3">
        <span className={`mt-0.5 rounded px-1.5 py-0.5 font-mono text-[10px] font-semibold ring-1 ring-inset ${style.badge}`}>
          {t.types[event.type]}
        </span>
        <div className="min-w-0 flex-1">
          <div className="truncate font-mono text-[13px] text-slate-200">{summarize(event) || event.type}</div>
          <div className="mt-0.5 font-mono text-[11px] text-slate-500">
            {formatTime(event.timestamp)}
            {event.durationMs !== null && event.durationMs !== undefined && (
              <span className={durationTone(event.durationMs)}> · {formatDuration(event.durationMs, locale)}</span>
            )}
            {event.statusCode ? <span className={statusTone(event.statusCode)}> · {event.statusCode}</span> : null}
          </div>
        </div>
        <button onClick={onClose} className="rounded p-1 text-slate-600 hover:bg-slate-800 hover:text-slate-300" title={t.details.close}>
          ✕
        </button>
      </header>

      <div className="flex-1 space-y-4 overflow-y-auto p-4">
        {event.sql && (
          <Block title={t.details.sql} copy={event.sql}>
            <pre className="overflow-x-auto whitespace-pre-wrap break-words rounded-lg border border-slate-800 bg-slate-950/80 p-3 font-mono text-[12px] leading-relaxed text-slate-300">
              {highlightSql(event.sql).map((token, index) => (
                <span
                  key={index}
                  className={
                    token.kind === 'keyword'
                      ? 'text-violet-300'
                      : token.kind === 'string'
                        ? 'text-emerald-300'
                        : token.kind === 'number'
                          ? 'text-amber-300'
                          : undefined
                  }
                >
                  {token.text}
                </span>
              ))}
            </pre>
          </Block>
        )}

        {event.parameters && (
          <Block title={t.details.parameters} copy={event.parameters}>
            <Code>{event.parameters}</Code>
          </Block>
        )}

        {event.requestBody && (
          <Block title={t.details.requestBody} copy={event.requestBody}>
            <Code>{prettyJson(event.requestBody) ?? event.requestBody}</Code>
          </Block>
        )}

        {event.responseBody && (
          <Block title={json ? t.details.responseBodyJson : t.details.responseBody} copy={event.responseBody}>
            <Code>{json ?? event.responseBody}</Code>
          </Block>
        )}

        {event.exception && <ExceptionBlock exception={event.exception} />}

        {event.type === 'http' && (
          <Block title={`${t.details.related} (${children.length})`}>
            {children.length === 0 ? (
              <p className="text-xs text-slate-600">{t.details.noRelated}</p>
            ) : (
              <div className="space-y-1">
                {children.map(child => (
                  <button
                    key={child.id}
                    onClick={() => onSelect(child)}
                    className="flex w-full items-center gap-2 rounded-lg border border-slate-800 bg-slate-950/60 px-2.5 py-1.5 text-left hover:border-slate-700 hover:bg-slate-900"
                  >
                    <span className={`h-1.5 w-1.5 shrink-0 rounded-full ${TYPE_STYLE[child.type].dot}`} />
                    <span className="truncate font-mono text-[12px] text-slate-400">{summarize(child)}</span>
                    <span className="ml-auto shrink-0 font-mono text-[11px] text-slate-600">
                      {formatDuration(child.durationMs, locale)}
                    </span>
                  </button>
                ))}
              </div>
            )}
          </Block>
        )}

        {parent && (
          <Block title={t.details.partOfRequest}>
            <button
              onClick={() => onSelect(parent)}
              className="flex w-full items-center gap-2 rounded-lg border border-slate-800 bg-slate-950/60 px-2.5 py-2 text-left hover:border-indigo-500/40 hover:bg-indigo-500/10"
            >
              <span className={`h-1.5 w-1.5 rounded-full ${TYPE_STYLE.http.dot}`} />
              <span className="truncate font-mono text-[12px] text-slate-300">
                {parent.method} {parent.path}
              </span>
              <span className={`ml-auto font-mono text-[11px] ${statusTone(parent.statusCode)}`}>{parent.statusCode}</span>
            </button>
          </Block>
        )}

        {event.category && (
          <Block title={t.details.category}>
            <Code>{event.category}</Code>
          </Block>
        )}

        <Block title={t.details.eventId}>
          <Code>{event.id}</Code>
        </Block>
      </div>

      {bundle && (
        <footer className="flex items-center gap-2 border-t border-slate-800 bg-slate-950/60 px-4 py-2">
          <ExportButton
            label={t.details.exportMarkdown}
            build={() => toMarkdown(bundle.request, bundle.related, t, locale)}
          />
          <ExportButton label={t.details.exportJson} build={() => toJsonExport(bundle.request, bundle.related)} />
          <span className="ml-auto font-mono text-[11px] text-slate-600">{t.details.events(bundle.related.length + 1)}</span>
        </footer>
      )}
    </aside>
  )
}

function ExceptionBlock({ exception }: { exception: string }) {
  const { t } = useI18n()
  const lines = stackFrames(exception)
  const [head, ...frames] = lines

  return (
    <Block title={t.details.exception} copy={exception}>
      <div className="rounded-lg border border-rose-500/20 bg-rose-500/5 p-3">
        <p className="font-mono text-[12px] font-semibold text-rose-300">{head}</p>
        <div className="mt-2 space-y-0.5">
          {frames.map((frame, index) => (
            <Frame key={index} frame={frame} />
          ))}
        </div>
      </div>
    </Block>
  )
}

function Frame({ frame }: { frame: string }) {
  const { t } = useI18n()
  const marker = ' in '
  const splitAt = frame.indexOf(marker)
  const isFrame = frame.startsWith('at ')
  const call = splitAt >= 0 ? frame.slice(0, splitAt) : frame
  const location = splitAt >= 0 ? frame.slice(splitAt + marker.length) : ''

  return (
    <button
      onClick={() => void navigator.clipboard?.writeText(frame)}
      title={t.details.copyFrame}
      className="block w-full rounded px-1 py-0.5 text-left font-mono text-[11px] hover:bg-rose-500/10"
    >
      <span className={`block truncate ${isFrame ? 'text-slate-400' : 'text-slate-500'}`}>{call}</span>
      {location && <span className="block truncate text-indigo-300/80">{location}</span>}
    </button>
  )
}

function ExportButton({ label, build }: { label: string; build: () => string }) {
  const [copied, setCopied] = useState(false)

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(build())
      setCopied(true)
      window.setTimeout(() => setCopied(false), 1200)
    } catch {
      // Clipboard access can be denied outside localhost; nothing else to do here.
    }
  }

  return (
    <button
      onClick={copy}
      className="rounded-lg border border-slate-800 bg-slate-950/80 px-2.5 py-1.5 text-[11px] text-slate-300 transition hover:border-indigo-500/40 hover:text-indigo-300"
    >
      {copied ? `${label} ✓` : label}
    </button>
  )
}

function Block({ title, copy, children }: { title: string; copy?: string; children: ReactNode }) {
  return (
    <section>
      <div className="mb-1.5 flex items-center justify-between">
        <h3 className="text-[11px] font-medium uppercase tracking-wider text-slate-500">{title}</h3>
        {copy && <CopyButton value={copy} />}
      </div>
      {children}
    </section>
  )
}

function CopyButton({ value }: { value: string }) {
  const { t } = useI18n()
  const [copied, setCopied] = useState(false)

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(value)
      setCopied(true)
      window.setTimeout(() => setCopied(false), 1200)
    } catch {
      // Clipboard access can be denied; the value is still on screen.
    }
  }

  return (
    <button onClick={copy} className="text-[11px] text-slate-600 hover:text-indigo-300">
      {copied ? t.details.copied : t.details.copy}
    </button>
  )
}

function Code({ children }: { children: string }) {
  return (
    <pre className="overflow-x-auto whitespace-pre-wrap break-words rounded-lg border border-slate-800 bg-slate-950/80 p-3 font-mono text-[12px] leading-relaxed text-slate-300">
      {children}
    </pre>
  )
}
