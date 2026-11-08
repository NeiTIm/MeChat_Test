import { act, render, screen, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { afterEach, beforeEach, expect, it, vi } from 'vitest'
import { MessageReply, MessageReactions, MessageReceipt } from '@/features/messages/MessageActions'
import { metadataOptions, metadataKey } from '@/features/messages/message-metadata'
import { handleMessageHint } from '@/features/messages/message-events'
import { queryClient } from '@/lib/query-client'
import { useAuthStore } from '@/stores/auth-store'
import { messagesApi } from '@/features/messages/api'
import { errorMessage } from '@/lib/errors'
import { http } from '@/lib/http'

vi.mock('@/features/messages/api', () => ({ messagesApi: { context: vi.fn(), receipts: vi.fn(), get: vi.fn() }, requestsApi: {} }))
const message = { id: 'm', conversationId: 'room', sequence: 30, senderUserId: 'actor', replyToMessageId: 'source' }
const mount = child => render(<QueryClientProvider client={queryClient}>{child}</QueryClientProvider>)
beforeEach(() => {
  useAuthStore.getState().setAccess({ userId: 'actor', accessToken: 'test-only' })
  messagesApi.context.mockResolvedValue({ reply: { available: true, content: 'Trích dẫn tổng hợp' }, reactions: [], myReaction: null })
  messagesApi.receipts.mockResolvedValue({ status: 'Delivered', deliveredCount: 1, seenCount: 0 })
})
afterEach(() => { queryClient.clear(); useAuthStore.getState().clear(); vi.clearAllMocks(); vi.restoreAllMocks(); vi.unstubAllGlobals() })

it('deduplicates reply/reaction observers and reuses fresh data on remount', async () => {
  const first = mount(<><MessageReply message={message} /><MessageReactions message={message} /></>)
  await screen.findByText('Trích dẫn tổng hợp')
  expect(messagesApi.context).toHaveBeenCalledTimes(1)
  expect(messagesApi.get).not.toHaveBeenCalled()
  first.unmount()
  mount(<MessageReactions message={message} />)
  await act(async () => {})
  expect(messagesApi.context).toHaveBeenCalledTimes(1)
})

it('does not load reply context without replyTo, receipts for incoming rows, or offscreen metadata', async () => {
  const callbacks = []
  vi.stubGlobal('IntersectionObserver', class { constructor(callback) { callbacks.push(callback) } observe() {} disconnect() {} })
  mount(<article data-message-id="m"><MessageReply message={{ ...message, replyToMessageId: null }} /><MessageReactions message={message} /><MessageReceipt message={message} own={false} /></article>)
  await act(async () => {})
  expect(messagesApi.context).not.toHaveBeenCalled(); expect(messagesApi.receipts).not.toHaveBeenCalled()
  await act(async () => callbacks.forEach(callback => callback([{ isIntersecting: true }])))
  await waitFor(() => expect(messagesApi.context).toHaveBeenCalledTimes(1))
  expect(messagesApi.receipts).not.toHaveBeenCalled()
  vi.unstubAllGlobals()
})

it('partitions metadata by account and conversation, preserving in-flight cancellation', () => {
  const options = metadataOptions('receipt', 'actor', message, true)
  expect(options.queryKey).not.toEqual(metadataKey('receipt', 'another-account', message))
  expect(options.queryKey).not.toEqual(metadataKey('receipt', 'actor', { ...message, conversationId: 'other-room' }))
  expect(options.retry).toBe(false); expect(options.refetchOnWindowFocus).toBe(false)
  const abort = new AbortController()
  options.queryFn({ signal: abort.signal })
  expect(messagesApi.receipts).toHaveBeenCalledWith('m', abort.signal)
})

it('honors Retry-After without retrying or extending cooldown on background invalidation', async () => {
  const client = new QueryClient(), error = { config: { method: 'get' }, response: { status: 429, headers: { 'retry-after': '2' } } }
  let time = 10000; vi.spyOn(Date, 'now').mockImplementation(() => time)
  messagesApi.receipts.mockRejectedValueOnce(error).mockResolvedValue({ status: 'Seen', deliveredCount: 1, seenCount: 1 })
  const options = metadataOptions('receipt', 'actor', message, true, client)
  await expect(client.fetchQuery(options)).rejects.toBe(error)
  time += 1000; await expect(client.fetchQuery(options)).rejects.toBe(error)
  expect(messagesApi.receipts).toHaveBeenCalledTimes(1)
  time += 1100; expect((await client.fetchQuery(options)).status).toBe('Seen')
  expect(messagesApi.receipts).toHaveBeenCalledTimes(2); client.clear()
})

it('invalidates only relevant account/conversation receipts; seen by self only updates unread projections', async () => {
  const client = new QueryClient()
  function cached(kind, actor, row) { const options = metadataOptions(kind, actor, row, true, client); const q = client.getQueryCache().build(client, options); q.setData({ status: 'Delivered' }); return q }
  const matching = cached('receipt', 'actor', message), otherRoom = cached('receipt', 'actor', { ...message, conversationId: 'other' })
  const otherAccount = cached('receipt', 'other', message), context = cached('context', 'actor', message)
  const history = ['workspace', 'private', 'room', 'messages']; client.setQueryData(history, { pages: [] })
  await handleMessageHint(client, 'MessageSeen', { conversationId: 'room', userId: 'peer', lastReadSequence: 30 }, 'actor')
  expect(matching.state.isInvalidated).toBe(true); expect(otherRoom.state.isInvalidated).toBe(false); expect(otherAccount.state.isInvalidated).toBe(false)
  expect(context.state.isInvalidated).toBe(false); expect(client.getQueryState(history).isInvalidated).toBe(false)
  matching.setData({ status: 'Seen' })
  await handleMessageHint(client, 'MessageSeen', { conversationId: 'room', userId: 'actor', lastReadSequence: 30 }, 'actor')
  expect(matching.state.isInvalidated).toBe(false); client.clear()
})

it('reaction/edit hints refresh target and dependent replies; recall sanitizes immediately without clearing unrelated context', async () => {
  const client = new QueryClient()
  const add = row => { const q = client.getQueryCache().build(client, metadataOptions('context', 'actor', row, true, client)); q.setData({ reply: { content: 'Synthetic quote' } }); return q }
  const dependent = add({ ...message, id: 'reply', replyToMessageId: 'target' }), unrelated = add({ ...message, id: 'other', replyToMessageId: null })
  const versions = ['workspace', 'editHistory', 'target'], search = ['workspace', 'messageSearch', 'room', 'synthetic'], otherSearch = ['workspace', 'messageSearch', 'other-room', 'synthetic']
  for (const key of [versions, search, otherSearch]) client.setQueryData(key, { items: [] })
  const history = ['workspace', 'private', 'room', 'messages']; client.setQueryData(history, { pages: [{ items: [{ id: 'target', senderUserId: 'actor', sequence: 1, content: 'Synthetic source' }] }] })
  await handleMessageHint(client, 'MessageEdited', { conversationId: 'room', messageId: 'target' }, 'actor')
  expect(dependent.state.isInvalidated).toBe(true); expect(unrelated.state.isInvalidated).toBe(false)
  expect(client.getQueryState(versions).isInvalidated).toBe(true)
  expect(client.getQueryState(search).isInvalidated).toBe(true); expect(client.getQueryState(otherSearch).isInvalidated).toBe(false)
  await handleMessageHint(client, 'MessageRecalled', { conversationId: 'room', messageId: 'target' }, 'actor')
  expect(dependent.state.data).toBeUndefined(); expect(unrelated.state.data).toBeDefined()
  expect(client.getQueryData(history).pages[0].items[0]).toMatchObject({ recalled: true, content: null, attachments: [] }); client.clear()
})

it('describes 429 GET as load throttling while leaving mutation errors distinct', () => {
  expect(errorMessage({ config: { method: 'get' }, response: { status: 429 } })).toContain('giới hạn lượt tải')
  expect(errorMessage({ config: { method: 'post' }, response: { status: 429 } })).toContain('thao tác')
})

it('the real HTTP interceptor rejects 429 once without refreshing or replaying', async () => {
  const original = http.defaults.adapter
  let calls = 0
  http.defaults.adapter = async config => { calls++; throw { config, response: { status: 429 } } }
  try { await expect(http.get('/test-only-no-network')).rejects.toMatchObject({ response: { status: 429 } }); expect(calls).toBe(1) }
  finally { http.defaults.adapter = original }
  expect(queryClient.getDefaultOptions().queries.retry(0, { response: { status: 429 } })).toBe(false)
})
