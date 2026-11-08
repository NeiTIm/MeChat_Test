import { act, cleanup, render, screen } from '@testing-library/react'
import { afterEach, beforeEach, expect, it, vi } from 'vitest'
import { RealtimeProvider, useRealtime } from '@/app/providers/RealtimeProvider'
import { queryClient } from '@/lib/query-client'
import { useAuthStore } from '@/stores/auth-store'
import { clearSession } from '@/lib/http'

const { connections } = vi.hoisted(() => ({ connections: [] }))
vi.mock('@/lib/http', () => ({ accessToken: vi.fn(), clearSession: vi.fn() }))
vi.mock('@microsoft/signalr', () => ({ LogLevel: { None: 0 }, HubConnectionBuilder: class {
  withUrl() { return this }
  configureLogging() { return this }
  withAutomaticReconnect() { return this }
  build() {
    const connection = { state: 'Connected', handlers: {}, start: vi.fn(async () => {}), stop: vi.fn(async () => {}), invoke: vi.fn(async () => true), on(name, callback) { this.handlers[name] = callback }, onclose(callback) { this.closed = callback }, onreconnecting(callback) { this.reconnecting = callback }, onreconnected(callback) { this.reconnected = callback } }
    connections.push(connection)
    return connection
  }
} }))
let current
function Consumer() { current = useRealtime(); return <span>{current.status} {current.typing.length}</span> }
beforeEach(() => {
  connections.length = 0
  useAuthStore.getState().setAccess({ userId: 'test-actor', accessToken: 'test-only' })
  vi.useFakeTimers()
})
afterEach(() => { cleanup(); queryClient.clear(); useAuthStore.getState().clear(); vi.useRealTimers(); vi.restoreAllMocks() })
async function mount() { const rendered = render(<RealtimeProvider><Consumer /></RealtimeProvider>); await act(async () => {}); return rendered }
it('deduplicates message hints and discards protected cached content on permissions changes', async () => {
  await mount()
  const invalidate = vi.spyOn(queryClient, 'invalidateQueries')
  await act(async () => { connections[0].handlers.MessageCreated({ eventId: 'event-1' }); connections[0].handlers.MessageCreated({ eventId: 'event-1' }); vi.advanceTimersByTime(81) })
  expect(invalidate).toHaveBeenCalledTimes(1)
  queryClient.setQueryData(['workspace', 'private', 'test-thread', 'messages'], { content: 'private test content' })
  await act(async () => connections[0].handlers.PermissionsChanged({ eventId: 'permission-1' }))
  expect(queryClient.getQueryData(['workspace', 'private', 'test-thread', 'messages'])).toBeUndefined()
})
it('expires typing hints and permits only the supported conversation invocations', async () => {
  await mount()
  await act(async () => connections[0].handlers.TypingChanged({ conversationId: 'thread', actorId: 'other', isTyping: true, expiresAt: new Date(Date.now() + 5000).toISOString() }))
  expect(current.typing).toHaveLength(1)
  await act(async () => vi.advanceTimersByTime(6000))
  expect(current.typing).toHaveLength(0)
  await current.invoke('StartTyping', 'thread')
  expect(connections[0].invoke).toHaveBeenCalledWith('StartTyping', 'thread')
  expect(await current.invoke('UnsupportedMethod', 'thread')).toBe(false)
})
it('does not rerender idle consumers on every typing TTL tick', async () => {
  let renders = 0
  function IdleConsumer() { useRealtime(); renders++; return null }
  render(<RealtimeProvider><IdleConsumer /></RealtimeProvider>)
  await act(async () => {})
  const initial = renders
  await act(async () => vi.advanceTimersByTime(5000))
  expect(renders).toBe(initial)
})
it('ends revoked sessions and ignores late events after connection cleanup', async () => {
  const rendered = await mount()
  await act(async () => connections[0].handlers.SessionRevoked({ eventId: 'revocation' }))
  expect(clearSession).toHaveBeenCalled()
  rendered.unmount()
  expect(connections[0].stop).toHaveBeenCalledTimes(1)
  const invalidate = vi.spyOn(queryClient, 'invalidateQueries')
  await act(async () => connections[0].handlers.MessageCreated({ eventId: 'late-event' }))
  expect(invalidate).not.toHaveBeenCalled()
  expect(screen.queryByText(/connected/)).not.toBeInTheDocument()
})

it('reconciles all three BCR metadata events and HTTP fallback signals', async () => {
  await mount()
  const invalidate = vi.spyOn(queryClient, 'invalidateQueries')
  await act(async () => {
    for (const name of ['NotificationDeleted', 'NotificationRestored', 'FriendRequestsUnreadChanged'])
      connections[0].handlers[name]({ eventId: name })
  })
  expect(invalidate).toHaveBeenCalledTimes(3)
  await act(async () => { window.dispatchEvent(new Event('focus')); window.dispatchEvent(new Event('online')); connections[0].reconnected() })
  expect(invalidate).toHaveBeenCalledTimes(6)
})

it('coalesces receipt bursts without invalidating history and drops queued hints after cleanup', async () => {
  const rendered = await mount()
  const history = ['workspace', 'private', 'room', 'messages']
  queryClient.setQueryData(history, { pages: [{ items: [] }] })
  const invalidate = vi.spyOn(queryClient, 'invalidateQueries')
  await act(async () => {
    for (let i = 0; i < 20; i++) connections[0].handlers.MessageSeen({ eventId: `seen-${i}`, conversationId: 'room', userId: 'peer', lastReadSequence: 20 - i })
    vi.advanceTimersByTime(81)
  })
  expect(invalidate).toHaveBeenCalledTimes(1)
  expect(queryClient.getQueryState(history).isInvalidated).toBe(false)
  connections[0].handlers.MessageReactionChanged({ conversationId: 'room', messageId: 'queued' })
  rendered.unmount(); invalidate.mockClear()
  await act(async () => vi.advanceTimersByTime(100))
  expect(invalidate).not.toHaveBeenCalled()
})
