import { afterEach, beforeEach, expect, it, vi } from 'vitest'
import { createSeenQueue } from '@/features/friends/seen-queue'
import { notificationNextPage } from '@/features/notifications/use-notifications'

let environment, doc, focus, current, send, confirmed, failed, queue
const node = (top = 0, height = 100) => ({ isConnected: true, getBoundingClientRect: () => ({ top, bottom: top + height, height, left: 0, right: 300, width: 300 }) })
beforeEach(() => {
  vi.useFakeTimers(); focus = true; current = true
  environment = Object.assign(new EventTarget(), { innerHeight: 600, innerWidth: 390 })
  doc = Object.assign(new EventTarget(), { visibilityState: 'visible', hasFocus: () => focus })
  send = vi.fn(async () => {}); confirmed = vi.fn(); failed = vi.fn()
  queue = createSeenQueue({ environment, doc, send, confirmed, failed, isCurrent: () => current })
})
afterEach(() => { queue.stop(); vi.useRealTimers() })
it('requires 25% visible continuously for 500ms and does not infer seen from registration', async () => {
  const row = node(580); queue.register('a', row)
  await vi.advanceTimersByTimeAsync(1000); expect(send).not.toHaveBeenCalled()
  row.getBoundingClientRect = node(575).getBoundingClientRect; environment.dispatchEvent(new Event('scroll'))
  await vi.advanceTimersByTimeAsync(499); expect(send).not.toHaveBeenCalled()
  await vi.advanceTimersByTimeAsync(2); expect(send.mock.calls[0][0]).toEqual(['a']); expect(confirmed).toHaveBeenCalledOnce()
})
it('resets dwell when row leaves the viewport or tab loses focus', async () => {
  queue.register('a', node()); await vi.advanceTimersByTimeAsync(300)
  focus = false; environment.dispatchEvent(new Event('blur')); await vi.advanceTimersByTimeAsync(1000)
  expect(send).not.toHaveBeenCalled(); focus = true; environment.dispatchEvent(new Event('focus'))
  await vi.advanceTimersByTimeAsync(499); expect(send).not.toHaveBeenCalled()
  await vi.advanceTimersByTimeAsync(2); expect(send).toHaveBeenCalledOnce()
})
it('does not mark hidden background tabs, disconnected rows or another account', async () => {
  doc.visibilityState = 'hidden'; queue.register('hidden', node()); await vi.advanceTimersByTimeAsync(1000)
  doc.visibilityState = 'visible'; current = false; doc.dispatchEvent(new Event('visibilitychange'))
  await vi.advanceTimersByTimeAsync(1000); expect(send).not.toHaveBeenCalled()
  current = true; const detached = node(); detached.isConnected = false; queue.register('detached', detached)
  queue.register('hidden', null); await vi.advanceTimersByTimeAsync(1000); expect(send).not.toHaveBeenCalled()
})
it('handles a row taller than viewport using a viewport-relative visible threshold', async () => {
  queue.register('tall', node(400, 10000)); await vi.advanceTimersByTimeAsync(501)
  expect(send.mock.calls[0][0]).toEqual(['tall'])
})
it('chunks 205 eligible IDs without duplicate/inflight submissions', async () => {
  for (let i = 0; i < 205; i++) queue.register(`r${i}`, node())
  await vi.advanceTimersByTimeAsync(510)
  expect(send.mock.calls.map(([ids]) => ids.length)).toEqual([100, 100, 5])
  expect(new Set(send.mock.calls.flatMap(([ids]) => ids)).size).toBe(205)
  environment.dispatchEvent(new Event('scroll')); await vi.advanceTimersByTimeAsync(1000)
  expect(send).toHaveBeenCalledTimes(3)
})
it('aborts inflight on stop and ignores late confirmations', async () => {
  let resolve
  send.mockImplementation(() => new Promise(done => { resolve = done }))
  queue.register('a', node()); await vi.advanceTimersByTimeAsync(501)
  const signal = send.mock.calls[0][1]; queue.stop(); expect(signal.aborted).toBe(true)
  resolve(); await Promise.resolve(); expect(confirmed).not.toHaveBeenCalled()
})
it('keeps errors visible and revalidates viewport on explicit retry', async () => {
  send.mockRejectedValueOnce(new Error('test-only failure'))
  const row = node(); queue.register('a', row); await vi.advanceTimersByTimeAsync(501)
  expect(failed).toHaveBeenCalledOnce(); expect(confirmed).not.toHaveBeenCalled()
  row.getBoundingClientRect = node(900).getBoundingClientRect; queue.retry()
  await vi.advanceTimersByTimeAsync(1000); expect(send).toHaveBeenCalledOnce()
  row.getBoundingClientRect = node().getBoundingClientRect; environment.dispatchEvent(new Event('scroll'))
  await vi.advanceTimersByTimeAsync(501); expect(send).toHaveBeenCalledTimes(2); expect(confirmed).toHaveBeenCalledOnce()
})
it('does not requeue rows while their confirmation is pending', async () => {
  send.mockImplementation(() => new Promise(() => {}))
  queue.register('a', node()); await vi.advanceTimersByTimeAsync(501)
  environment.dispatchEvent(new Event('scroll')); await vi.advanceTimersByTimeAsync(1000)
  expect(send).toHaveBeenCalledOnce()
})
it('preserves seven-digit UTC cursors across empty visible pages', () => {
  const nextCursor = '2026-11-08T10:00:00.1234567Z'
  expect(notificationNextPage({ items: [], hasMore: true, nextCursor, nextCursorId: 'id' })).toEqual({ before: nextCursor, beforeId: 'id' })
  expect(notificationNextPage({ items: [], hasMore: false })).toBeUndefined()
})
