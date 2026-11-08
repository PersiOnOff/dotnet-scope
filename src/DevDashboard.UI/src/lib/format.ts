import type { EventType, TelemetryEvent } from '../types'

export const API = '/_dashboard/api'
export const HUB = '/_dashboard/hub'
export const MAX_EVENTS = 1000

export function formatDuration(ms?: number | null): string {
  if (ms === null || ms === undefined) return '—'
  if (ms < 1000) return `${ms} ms`
  return `${(ms / 1000).toFixed(2)} s`
}

export function formatTime(timestamp: string): string {
  const date = new Date(timestamp)
  return date.toLocaleTimeString([], { hour12: false }) + '.' + String(date.getMilliseconds()).padStart(3, '0')
}

export function statusTone(status?: number | null): string {
  if (!status) return 'text-slate-500'
  if (status >= 500) return 'text-rose-400'
  if (status >= 400) return 'text-amber-400'
  if (status >= 300) return 'text-sky-400'
  return 'text-emerald-400'
}

export function durationTone(ms?: number | null): string {
  if (ms === null || ms === undefined) return 'text-slate-500'
  if (ms >= 500) return 'text-rose-400'
  if (ms >= 100) return 'text-amber-400'
  return 'text-emerald-400'
}

export const TYPE_STYLE: Record<EventType, { label: string; badge: string; dot: string }> = {
  http: { label: 'HTTP', badge: 'bg-indigo-500/10 text-indigo-300 ring-indigo-500/30', dot: 'bg-indigo-400' },
  sql: { label: 'SQL', badge: 'bg-violet-500/10 text-violet-300 ring-violet-500/30', dot: 'bg-violet-400' },
  log: { label: 'LOG', badge: 'bg-sky-500/10 text-sky-300 ring-sky-500/30', dot: 'bg-sky-400' },
  exception: { label: 'ERR', badge: 'bg-rose-500/10 text-rose-300 ring-rose-500/30', dot: 'bg-rose-400' },
}

export function summarize(event: TelemetryEvent): string {
  switch (event.type) {
    case 'http':
      return `${event.method ?? ''} ${event.path ?? ''}`.trim()
    case 'sql':
      return (event.sql ?? '').replace(/\s+/g, ' ').trim()
    case 'log':
      return `[${event.logLevel}] ${event.message ?? ''}`
    case 'exception':
      return event.message ?? (event.exception ?? '').split('\n')[0]
  }
}

export function prettyJson(value?: string | null): string | null {
  if (!value) return null
  const trimmed = value.trim()
  if (!trimmed || (trimmed[0] !== '{' && trimmed[0] !== '[')) return null
  try {
    return JSON.stringify(JSON.parse(trimmed), null, 2)
  } catch {
    return null
  }
}

/** Splits SQL into plain/keyword/number/literal tokens so it can be rendered with colours. */
export function highlightSql(sql: string): { text: string; kind: 'plain' | 'keyword' | 'string' | 'number' }[] {
  const parts: { text: string; kind: 'plain' | 'keyword' | 'string' | 'number' }[] = []
  const pattern = /('(?:[^']|'')*')|(\b\d+(?:\.\d+)?\b)|(\b(?:SELECT|FROM|WHERE|INNER|LEFT|RIGHT|OUTER|JOIN|ON|GROUP|BY|ORDER|HAVING|INSERT|INTO|VALUES|UPDATE|SET|DELETE|CREATE|TABLE|ALTER|DROP|AND|OR|NOT|NULL|AS|LIMIT|OFFSET|EXISTS|CASE|WHEN|THEN|ELSE|END|DISTINCT|COUNT|SUM|AVG|MIN|MAX)\b)/gi
  let last = 0
  for (const match of sql.matchAll(pattern)) {
    const index = match.index ?? 0
    if (index > last) parts.push({ text: sql.slice(last, index), kind: 'plain' })
    const kind = match[1] ? 'string' : match[2] ? 'number' : 'keyword'
    parts.push({ text: match[0], kind })
    last = index + match[0].length
  }
  if (last < sql.length) parts.push({ text: sql.slice(last), kind: 'plain' })
  return parts
}

export function stackFrames(exception?: string | null): string[] {
  if (!exception) return []
  return exception
    .split('\n')
    .map(line => line.trim())
    .filter(line => line.length > 0)
}
