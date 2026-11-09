import { act, renderHook } from '@testing-library/react'
import { afterEach, expect, it, vi } from 'vitest'
import { useConversationTyping } from '@/features/conversations/hooks/use-conversation-typing'

afterEach(() => vi.useRealTimers())

it('joins once, preserves the typing throttle and cleans up the previous room before joining', () => {
  vi.useFakeTimers()
  vi.setSystemTime(new Date('2026-11-09T12:00:00Z'))
  const invoke = vi.fn()
  const initialProps = { id: 'first', status: 'connected', invoke, requestMode: false }
  const { result, rerender, unmount } = renderHook(useConversationTyping, { initialProps })
  rerender({ ...initialProps })
  expect(invoke.mock.calls).toEqual([['JoinConversation', 'first']])

  act(() => {
    result.current(true)
    result.current(true)
    vi.advanceTimersByTime(2500)
    result.current(true)
    result.current(false)
  })
  expect(invoke.mock.calls.slice(1)).toEqual([
    ['StartTyping', 'first'],
    ['StartTyping', 'first'],
    ['StopTyping', 'first'],
  ])

  rerender({ ...initialProps, id: 'second' })
  expect(invoke.mock.calls.slice(-3)).toEqual([
    ['StopTyping', 'first'],
    ['LeaveConversation', 'first'],
    ['JoinConversation', 'second'],
  ])
  unmount()
  expect(invoke.mock.calls.slice(-2)).toEqual([
    ['StopTyping', 'second'],
    ['LeaveConversation', 'second'],
  ])
})

it('does not publish typing in message requests while keeping the existing room lifecycle', () => {
  const invoke = vi.fn()
  const { result } = renderHook(() =>
    useConversationTyping({ id: 'request', status: 'connected', invoke, requestMode: true }),
  )
  act(() => {
    result.current(true)
    result.current(false)
  })
  expect(invoke.mock.calls).toEqual([['JoinConversation', 'request']])
})

it('leaves on disconnect and rejoins once when the connection returns', () => {
  const invoke = vi.fn()
  const initialProps = { id: 'room', status: 'connected', invoke, requestMode: false }
  const { rerender } = renderHook(useConversationTyping, { initialProps })
  rerender({ ...initialProps, status: 'reconnecting' })
  rerender({ ...initialProps, status: 'reconnecting' })
  rerender(initialProps)
  expect(invoke.mock.calls).toEqual([
    ['JoinConversation', 'room'],
    ['StopTyping', 'room'],
    ['LeaveConversation', 'room'],
    ['JoinConversation', 'room'],
  ])
})
