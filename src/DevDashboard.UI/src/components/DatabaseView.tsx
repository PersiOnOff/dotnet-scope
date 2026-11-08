import { useEffect, useMemo, useState } from 'react'
import type { DataSourceInfo, TablePage } from '../types'
import { fetchTable } from '../lib/database'
import { useI18n } from '../i18n'

type Props = {
  sources: DataSourceInfo[]
  loaded: boolean
  onRefresh: () => void
}

const LIMITS = [25, 50, 100, 250]
const CONTROL =
  'rounded-lg border border-slate-800 bg-slate-950/80 px-2.5 py-1.5 text-sm text-slate-300 focus:border-indigo-500/60 focus:outline-none'

export default function DatabaseView({ sources, loaded, onRefresh }: Props) {
  const { t } = useI18n()
  const [selected, setSelected] = useState<{ source: string; table: string } | null>(null)
  const [limit, setLimit] = useState(50)
  const [page, setPage] = useState<TablePage | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const tables = useMemo(() => sources.flatMap(source => source.tables), [sources])

  // Keep the selection valid: first table by default, or drop it when the schema changed.
  useEffect(() => {
    if (selected && tables.some(table => table.source === selected.source && table.name === selected.table)) return

    const first = tables[0]
    setSelected(first ? { source: first.source, table: first.name } : null)
  }, [tables, selected])

  useEffect(() => {
    if (!selected) {
      setPage(null)
      return
    }

    let cancelled = false
    setLoading(true)
    setError(null)

    fetchTable(selected.source, selected.table, limit)
      .then(result => {
        if (!cancelled) setPage(result)
      })
      .catch((cause: unknown) => {
        if (cancelled) return
        setPage(null)
        setError(cause instanceof Error ? cause.message : String(cause))
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [selected, limit])

  if (!loaded) {
    return <div className="flex flex-1 items-center justify-center text-sm text-slate-500">{t.db.loading}</div>
  }

  if (sources.length === 0) {
    return <NoSources />
  }

  return (
    <div className="flex min-h-0 flex-1">
      <aside className="w-60 shrink-0 overflow-y-auto border-r border-slate-800 bg-slate-950/40">
        <div className="px-4 py-3 text-[11px] uppercase tracking-wider text-slate-500">{t.db.tables}</div>
        {sources.map(source => (
          <div key={source.name} className="mb-3">
            <div className="truncate px-4 py-1 font-mono text-[11px] text-slate-500">{source.name}</div>
            {source.tables.map(table => {
              const isSelected = selected?.source === table.source && selected?.table === table.name
              return (
                <button
                  key={`${table.source}.${table.name}`}
                  onClick={() => setSelected({ source: table.source, table: table.name })}
                  title={table.schema ? `${table.schema}.${table.name}` : table.name}
                  className={`flex w-full items-center gap-2 px-4 py-1.5 text-left font-mono text-[12px] transition ${
                    isSelected ? 'bg-indigo-500/10 text-indigo-300' : 'text-slate-400 hover:bg-slate-900'
                  }`}
                >
                  <span className="truncate">{table.name}</span>
                </button>
              )
            })}
            {source.tables.length === 0 && <div className="px-4 py-1 text-[11px] text-slate-600">—</div>}
          </div>
        ))}
      </aside>

      <section className="flex min-w-0 flex-1 flex-col">
        <header className="flex flex-wrap items-center gap-2 border-b border-slate-800 bg-slate-900/40 px-4 py-2.5">
          <div className="min-w-0">
            <div className="truncate font-mono text-sm text-slate-200">
              {selected ? `${selected.source}.${selected.table}` : t.db.title}
            </div>
            <div className="text-[11px] text-slate-600">{t.db.subtitle}</div>
          </div>

          <div className="ml-auto flex items-center gap-2">
            {page && (
              <span className="font-mono text-[11px] text-slate-600">
                {t.db.rows(page.rows.length)}
                {page.truncated ? ` · ${t.db.truncated}` : ''}
              </span>
            )}

            <select value={limit} onChange={event => setLimit(Number(event.target.value))} className={CONTROL}>
              {LIMITS.map(value => (
                <option key={value} value={value}>
                  {t.db.limit}: {value}
                </option>
              ))}
            </select>

            <button onClick={onRefresh} className="rounded-lg border border-slate-800 bg-slate-950/80 px-2.5 py-1.5 text-sm text-slate-300 hover:border-indigo-500/40 hover:text-indigo-300">
              {t.db.refresh}
            </button>
          </div>
        </header>

        <div className="min-h-0 flex-1 overflow-auto">
          {error ? (
            <div className="m-4 rounded-lg border border-rose-500/30 bg-rose-500/5 p-4">
              <p className="text-sm font-medium text-rose-300">{t.db.errorTitle}</p>
              <p className="mt-1 font-mono text-[12px] text-rose-200/80">{error}</p>
            </div>
          ) : !page ? (
            <div className="p-6 text-sm text-slate-500">{loading ? t.db.loading : t.db.selectTable}</div>
          ) : (
            <TableGrid page={page} />
          )}
        </div>
      </section>
    </div>
  )
}

function TableGrid({ page }: { page: TablePage }) {
  const { t } = useI18n()

  return (
    <table className="min-w-full border-collapse text-left font-mono text-[12px]">
      <thead className="sticky top-0 z-10 bg-slate-900">
        <tr>
          {page.columns.map(column => (
            <th key={column} className="whitespace-nowrap border-b border-slate-800 px-3 py-2 font-medium text-slate-400">
              {column}
            </th>
          ))}
        </tr>
      </thead>
      <tbody>
        {page.rows.length === 0 ? (
          <tr>
            <td colSpan={Math.max(page.columns.length, 1)} className="px-3 py-6 text-center text-slate-600">
              {t.db.emptyTable}
            </td>
          </tr>
        ) : (
          page.rows.map((row, index) => (
            <tr key={index} className="hover:bg-slate-900/60">
              {row.map((value, cell) => (
                <td key={cell} className="max-w-[30rem] truncate border-b border-slate-900/60 px-3 py-1.5 text-slate-300">
                  {value === null ? <span className="italic text-slate-600">{t.db.nullValue}</span> : String(value)}
                </td>
              ))}
            </tr>
          ))
        )}
      </tbody>
    </table>
  )
}

function NoSources() {
  const { t } = useI18n()

  return (
    <div className="flex flex-1 items-center justify-center p-8">
      <div className="max-w-lg space-y-3 text-center">
        <div className="mx-auto grid h-12 w-12 place-items-center rounded-xl bg-slate-900 text-xl text-slate-500 ring-1 ring-inset ring-slate-800">
          ⌸
        </div>
        <h2 className="text-base font-semibold text-slate-200">{t.db.noSourcesTitle}</h2>
        <p className="text-sm text-slate-500">{t.db.noSourcesBody}</p>
        <pre className="overflow-x-auto rounded-lg border border-slate-800 bg-slate-950/80 p-3 text-left font-mono text-[12px] text-slate-400">
          {t.db.noSourcesCode}
        </pre>
      </div>
    </div>
  )
}
