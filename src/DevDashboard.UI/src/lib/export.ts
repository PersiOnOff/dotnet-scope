import type { TelemetryEvent } from '../types'
import type { Locale, Messages } from '../i18n'
import { formatDuration, prettyJson } from './format'

/**
 * Renders one request and everything it caused as a markdown block that can be pasted into an
 * issue, a PR comment or a chat without losing the SQL and the stack trace.
 */
export function toMarkdown(request: TelemetryEvent, related: TelemetryEvent[], t: Messages, locale: Locale): string {
  const lines: string[] = []

  const status = request.statusCode ? ` → ${request.statusCode}` : ''
  const duration =
    request.durationMs !== null && request.durationMs !== undefined
      ? ` (${formatDuration(request.durationMs, locale)})`
      : ''
  lines.push(`### ${request.method ?? 'REQUEST'} ${request.path ?? ''}${status}${duration}`.trim())
  lines.push('')
  lines.push(`_${request.timestamp}_`)
  if (request.traceId) lines.push('', `trace: \`${request.traceId}\``)

  if (request.requestBody) {
    lines.push('', `#### ${t.markdown.requestBody}`, fence('', prettyJson(request.requestBody) ?? request.requestBody))
  }

  const sql = related.filter(event => event.type === 'sql')
  if (sql.length > 0) {
    lines.push('', `#### ${t.markdown.sql} (${sql.length})`)
    for (const event of sql) {
      const parameters = event.parameters
        ? '\n' + event.parameters.split('\n').map(line => `-- ${line}`).join('\n')
        : ''
      lines.push('', fence('sql', (event.sql ?? '(empty)') + parameters))
    }
  }

  const logs = related.filter(event => event.type === 'log')
  if (logs.length > 0) {
    lines.push('', `#### ${t.markdown.logs} (${logs.length})`)
    for (const event of logs) {
      const category = event.category ? ` \`${event.category}\`` : ''
      lines.push(`- **${event.logLevel ?? 'Log'}**${category} — ${event.message ?? ''}`)
    }
  }

  const exceptions = related.filter(event => event.type === 'exception' || event.exception)
  if (exceptions.length > 0) {
    lines.push('', `#### ${t.markdown.exception}`)
    for (const event of exceptions) {
      lines.push('', fence('text', event.exception ?? event.message ?? ''))
    }
  }

  if (request.responseBody) {
    const pretty = prettyJson(request.responseBody)
    lines.push('', `#### ${t.markdown.responseBody}`, fence(pretty ? 'json' : '', pretty ?? request.responseBody))
  }

  return lines.join('\n') + '\n'
}

/** Machine-readable export: the same shape the JSON API returns for `/_dashboard/api/requests/{id}`. */
export function toJsonExport(request: TelemetryEvent, related: TelemetryEvent[]): string {
  return JSON.stringify({ request, related }, null, 2)
}

function fence(language: string, body: string): string {
  // A body that already contains a fence would break the block; shrink it to two backticks.
  const safe = body.replace(/```/g, '``')
  return '```' + language + '\n' + safe + '\n```'
}
