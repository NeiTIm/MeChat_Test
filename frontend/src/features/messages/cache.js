import { queryClient } from '@/lib/query-client'

// Successful destructive actions remove sensitive content before HTTP revalidation.
export function sanitizeMessageCache(id, recall = false) {
  function sanitize(value) {
    if (!value || typeof value !== 'object') return value
    if (Array.isArray(value)) return value.map(sanitize).filter((item) => item !== null)
    if (Object.getPrototypeOf(value) !== Object.prototype) return value
    if (value.id === id && 'senderUserId' in value && 'sequence' in value) return recall ? { ...value, content: null, recalled: true, attachments: [] } : null
    return Object.fromEntries(Object.entries(value).map(([key, item]) => [key, sanitize(item)]))
  }
  // Touch message pages only: other workspace queries include authorized binary Blobs.
  queryClient.setQueriesData({ queryKey: ['workspace'], predicate: (query) => query.queryKey.at(-1) === 'messages' || query.queryKey[1] === 'messageSearch' }, sanitize)
  // A deleted/recalled source can appear in another message's reply preview.
  queryClient.resetQueries({ queryKey: ['workspace', 'messageContext'] })
  queryClient.resetQueries({ queryKey: ['workspace', 'message', id] })
  queryClient.resetQueries({ queryKey: ['workspace', 'editHistory', id] })
}
