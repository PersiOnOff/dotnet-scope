import type { DataSourceInfo, TablePage } from '../types'
import { API } from './format'

export async function fetchSources(): Promise<DataSourceInfo[]> {
  const response = await fetch(`${API}/db`)
  if (!response.ok) throw new Error(`HTTP ${response.status}`)
  return (await response.json()) as DataSourceInfo[]
}

export async function fetchTable(source: string, table: string, limit: number): Promise<TablePage> {
  const url = `${API}/db/${encodeURIComponent(source)}/${encodeURIComponent(table)}?limit=${limit}`
  const response = await fetch(url)
  if (!response.ok) {
    const body = (await response.json().catch(() => null)) as { error?: string } | null
    throw new Error(body?.error ?? `HTTP ${response.status}`)
  }
  return (await response.json()) as TablePage
}
