import { useCallback, useEffect, useRef, useState } from 'react'
import * as signalR from '@microsoft/signalr'
import type { TelemetryEvent } from '../types'
import { API, HUB, MAX_EVENTS } from '../lib/format'

export type ConnectionStatus = 'connecting' | 'connected' | 'reconnecting' | 'offline'

/**
 * Loads the backlog over HTTP once, then follows the SignalR stream.
 * While paused, incoming events are dropped so the list stops moving under the cursor.
 */
export function useTelemetry() {
  const [events, setEvents] = useState<TelemetryEvent[]>([])
  const [status, setStatus] = useState<ConnectionStatus>('connecting')
  const [paused, setPaused] = useState(false)
  const pausedRef = useRef(paused)
  pausedRef.current = paused

  useEffect(() => {
    let disposed = false

    fetch(`${API}/events`)
      .then(response => (response.ok ? response.json() : []))
      .then((backlog: TelemetryEvent[]) => {
        if (!disposed) setEvents(backlog.slice().reverse())
      })
      .catch(() => setStatus('offline'))

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB)
      .withAutomaticReconnect()
      .build()

    connection.on('ReceiveEvent', (event: TelemetryEvent) => {
      if (pausedRef.current) return
      setEvents(previous => [event, ...previous].slice(0, MAX_EVENTS))
    })

    connection.onreconnecting(() => setStatus('reconnecting'))
    connection.onreconnected(() => setStatus('connected'))
    connection.onclose(() => setStatus('offline'))

    connection
      .start()
      .then(() => setStatus('connected'))
      .catch(() => setStatus('offline'))

    return () => {
      disposed = true
      void connection.stop()
    }
  }, [])

  const clear = useCallback(async () => {
    try {
      await fetch(`${API}/events`, { method: 'DELETE' })
    } catch {
      // The dashboard is a dev tool; a failed clear must not break the UI.
    }
    setEvents([])
  }, [])

  return { events, status, paused, setPaused, clear }
}
