import { useCallback, useEffect, useRef, useState } from 'react'
import { queryClient } from '@/lib/query-client'
import { messagesApi, requestsApi } from './api'

export function useMessageReceipts({ id, userId, messages, requestId, enabled }) {
  const [error, setError] = useState(false)
  const [retry, setRetry] = useState(0)
  const delivered = useRef({ id, items: new Set() })
  const cursor = useRef({ id, sent: 0, wanted: 0, running: false })
  if (cursor.current.id !== id) cursor.current = { id, sent: 0, wanted: 0, running: false }
  if (delivered.current.id !== id) delivered.current = { id, items: new Set() }
  const onRead = useCallback(async (sequence) => {
    if (!enabled || !id) return
    const current = cursor.current
    current.wanted = Math.max(current.wanted, sequence)
    if (current.running || current.sent >= current.wanted) return
    current.running = true
    try {
      while (cursor.current === current && current.sent < current.wanted) {
        const next = current.wanted
        if (requestId) await requestsApi.read(requestId, next)
        else await messagesApi.read(id, next)
        current.sent = next
      }
      if (cursor.current === current) setError(false)
      queryClient.invalidateQueries({ queryKey: ['workspace', 'unreadSummary'] })
      queryClient.invalidateQueries({ queryKey: ['workspace', 'requestCount'] })
      queryClient.invalidateQueries({ queryKey: ['workspace', 'threadUnread', id] })
    } catch { if (cursor.current === current) setError(true) }
    finally { current.running = false }
  }, [enabled, id, requestId])
  useEffect(() => {
    if (!enabled || !id) return
    let disposed = false
    const deliveredItems = delivered.current.items
    const ids = messages.filter((message) => message.senderUserId !== userId && !deliveredItems.has(message.id)).map((message) => message.id)
    async function acknowledge() {
      for (let offset = 0; offset < ids.length; offset += 50) {
        if (disposed) return
        const batch = ids.slice(offset, offset + 50)
        try { await messagesApi.acknowledge(id, batch); batch.forEach((messageId) => deliveredItems.add(messageId)) }
        catch { if (!disposed) setError(true); return }
      }
    }
    acknowledge()
    return () => { disposed = true }
  }, [id, userId, messages, enabled, retry])
  return { onRead, error, retry: () => { setRetry((value) => value + 1); cursor.current.sent = 0; onRead(cursor.current.wanted) } }
}
