import { afterEach, expect, it } from 'vitest'
import { queryClient } from '@/lib/query-client'
import { sanitizeMessageCache } from '@/features/messages/cache'
afterEach(() => queryClient.clear())
it('removes deleted message pages and reply/detail/history content without corrupting authorized blobs', () => {
  const blob = new Blob(['private test file'], { type: 'text/plain' })
  const message = { id: 'target', senderUserId: 'sender', sequence: 1, content: 'private test message' }
  const history = ['workspace', 'private', 'thread', 'messages']
  queryClient.setQueryData(history, { pages: [{ items: [message], hasMore: false }], pageParams: [null] })
  queryClient.setQueryData(['workspace', 'file', 'file-id'], blob)
  queryClient.setQueryData(['workspace', 'message', 'target'], message)
  queryClient.setQueryData(['workspace', 'editHistory', 'target'], { versions: [{ content: message.content }] })
  queryClient.setQueryData(['workspace', 'messageContext', 'reply-id'], { reply: { available: true, content: message.content } })
  sanitizeMessageCache('target')
  expect(queryClient.getQueryData(history).pages[0].items).toEqual([])
  expect(queryClient.getQueryData(['workspace', 'file', 'file-id'])).toBe(blob)
  expect(queryClient.getQueryData(['workspace', 'message', 'target'])).toBeUndefined()
  expect(queryClient.getQueryData(['workspace', 'editHistory', 'target'])).toBeUndefined()
  expect(queryClient.getQueryData(['workspace', 'messageContext', 'reply-id'])).toBeUndefined()
})
