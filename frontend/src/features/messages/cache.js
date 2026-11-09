import { queryClient } from '@/lib/query-client'

// Successful destructive actions remove sensitive content before HTTP revalidation.
export function sanitizeMessageCache(id, recall = false, client = queryClient) {
  function sanitize(value) {
    if (!value || typeof value !== 'object') return value
    if (Array.isArray(value)) return value.map(sanitize).filter((item) => item !== null)
    if (Object.getPrototypeOf(value) !== Object.prototype) return value
    if (value.id === id && 'senderUserId' in value && 'sequence' in value)
      return recall ? { ...value, content: null, recalled: true, attachments: [] } : null
    return Object.fromEntries(Object.entries(value).map(([key, item]) => [key, sanitize(item)]))
  }
  // Touch message pages only: other workspace queries include authorized binary Blobs.
  client.setQueriesData(
    {
      queryKey: ['workspace'],
      predicate: (query) =>
        query.queryKey.at(-1) === 'messages' || query.queryKey[1] === 'messageSearch',
    },
    sanitize,
  )
  // A deleted/recalled source can appear in another message's reply preview.
  client.resetQueries({
    queryKey: ['workspace', 'messageContext'],
    predicate: (q) =>
      !q.meta?.messageMetadata || q.meta.messageId === id || q.meta.replyToMessageId === id,
  })
  client.resetQueries({ queryKey: ['workspace', 'message', id] })
  client.resetQueries({ queryKey: ['workspace', 'editHistory', id] })
}
