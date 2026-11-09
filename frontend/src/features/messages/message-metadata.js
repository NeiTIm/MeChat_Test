import { useEffect, useRef, useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useAuthStore } from '@/stores/auth-store'
import { queryClient } from '@/lib/query-client'
import { messagesApi } from './api'

const limitedUntil = new WeakMap()
const limitedQueries = new WeakMap()
function cooldown(error) {
  if (!limitedUntil.has(error)) {
    const headers = error.response?.headers
    const value =
      headers?.get?.('retry-after') ?? headers?.['retry-after'] ?? headers?.['Retry-After']
    const delay =
      value != null && Number.isFinite(Number(value))
        ? Number(value) * 1000
        : Date.parse(value) - Date.now()
    limitedUntil.set(error, Date.now() + (Number.isFinite(delay) ? Math.max(1000, delay) : 30000))
  }
  return limitedUntil.get(error)
}

export const metadataKey = (kind, userId, message) => [
  'workspace',
  kind === 'context' ? 'messageContext' : 'receipt',
  userId,
  message.conversationId,
  message.id,
]
export function metadataOptions(kind, userId, message, enabled, client = queryClient) {
  const queryKey = metadataKey(kind, userId, message)
  return {
    queryKey,
    enabled: !!userId && !!message.id && !!message.conversationId && enabled,
    staleTime: Infinity,
    gcTime: 5 * 60000,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    retry: false,
    retryOnMount: false,
    meta: {
      messageMetadata: true,
      kind,
      userId,
      messageId: message.id,
      conversationId: message.conversationId,
      sequence: message.sequence,
      replyToMessageId: message.replyToMessageId,
    },
    queryFn: async ({ signal }) => {
      // TanStack clears state.error before a no-data refetch; keep the deadline on
      // the query object's lifetime instead, not in a persistent account/URL map.
      const query = client.getQueryCache().find({ queryKey, exact: true })
      const limited = query && limitedQueries.get(query)
      if (limited && limited.until > Date.now()) throw limited.error
      try {
        return await messagesApi[kind === 'context' ? 'context' : 'receipts'](message.id, signal)
      } catch (error) {
        if (error?.response?.status === 429 && query)
          limitedQueries.set(query, { error, until: cooldown(error) })
        throw error
      }
    },
  }
}

// Observe the containing message, not the zero-height metadata placeholder.
function useNearMessage(enabled) {
  const ref = useRef(null)
  const [near, setNear] = useState(typeof IntersectionObserver === 'undefined')
  useEffect(() => {
    if (!enabled || !ref.current || typeof IntersectionObserver === 'undefined') return
    const row = ref.current.closest('[data-message-id]') || ref.current
    const observer = new IntersectionObserver(
      (entries) => setNear(entries.some((entry) => entry.isIntersecting)),
      {
        root: row.closest('.message-scroll'),
        rootMargin: '150px 0px',
        threshold: 0,
      },
    )
    observer.observe(row)
    return () => observer.disconnect()
  }, [enabled])
  return { ref, near }
}

export function useMessageMetadata(kind, message, enabled = true) {
  const client = useQueryClient()
  const userId = useAuthStore((state) => state.access?.userId)
  const { ref, near } = useNearMessage(enabled)
  const query = useQuery(metadataOptions(kind, userId, message, enabled && near, client))
  return { ref, query, userId, active: enabled && near }
}
