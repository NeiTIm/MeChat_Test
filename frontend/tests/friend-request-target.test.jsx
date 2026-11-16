import { act, fireEvent, render, screen } from '@testing-library/react'
import { QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter, useLocation } from 'react-router'
import { useRef } from 'react'
import { afterEach, beforeEach, expect, it, vi } from 'vitest'
import { useFriendRequestTarget } from '@/features/friends/hooks/use-friend-request-target'
import { useAuthStore } from '@/stores/auth-store'
import { NotificationCard } from '@/features/notifications/components/NotificationCard'
import { translate } from '@/i18n'
import { queryClient } from '@/lib/query-client'
import { notificationsApi } from '@/features/notifications/api'
import { friendsApi } from '@/features/friends/api'
import { NotificationPreview } from '@/features/notifications/NotificationPreview'

const request = { id: 'target', receiverUserId: 'actor', status: 'Pending' }
function Target({
  data = { incoming: [request] },
  pending = false,
  requestKey = 'first',
  id = 'target',
  userId = 'actor',
  error,
}) {
  const ref = useRef(null)
  const result = useFriendRequestTarget({
    requestId: id,
    requestKey,
    userId,
    containerRef: ref,
    enabled: true,
    requests: { data, isPending: pending, isFetching: pending, error },
  })
  return (
    <section ref={ref}>
      <span data-testid="highlight">{result.highlightedId}</span>
      <span data-testid="unavailable">{String(result.unavailable)}</span>
      {data?.incoming?.map((row) => (
        <article
          key={row.id}
          tabIndex={-1}
          data-testid={row.id}
          data-friend-request-id={row.id}
        />
      ))}
    </section>
  )
}
beforeEach(() => {
  useAuthStore.getState().setAccess({ userId: 'actor' })
  vi.useFakeTimers()
})
afterEach(() => {
  useAuthStore.getState().clear()
  vi.useRealTimers()
  vi.restoreAllMocks()
})

it('focuses the exact loaded request once, preserves position after seen refetch and clears highlight', () => {
  const scroll = vi.spyOn(Element.prototype, 'scrollIntoView')
  const view = render(<Target />)
  expect(screen.getByTestId('target')).toHaveFocus()
  expect(screen.getByTestId('highlight')).toHaveTextContent('target')
  view.rerender(<Target data={{ incoming: [{ ...request, seenAt: 'now' }] }} />)
  expect(scroll).toHaveBeenCalledTimes(1)
  act(() => vi.advanceTimersByTime(2500))
  expect(screen.getByTestId('highlight')).toBeEmptyDOMElement()
})
it('waits for incoming data and permits an explicit repeated navigation to the same request', () => {
  const scroll = vi.spyOn(Element.prototype, 'scrollIntoView')
  const view = render(
    <Target
      pending
      data={{ incoming: [] }}
    />,
  )
  expect(scroll).not.toHaveBeenCalled()
  view.rerender(<Target />)
  expect(screen.getByTestId('target')).toHaveFocus()
  view.rerender(<Target requestKey="second" />)
  expect(scroll).toHaveBeenCalledTimes(2)
})
it.each([
  { incoming: [] },
  { incoming: [{ ...request, receiverUserId: 'other' }] },
  { incoming: [{ ...request, status: 'Accepted' }] },
])('never focuses a missing, alien or completed request', (data) => {
  const scroll = vi.spyOn(Element.prototype, 'scrollIntoView')
  render(<Target data={data} />)
  expect(screen.getByTestId('unavailable')).toHaveTextContent('true')
  expect(scroll).not.toHaveBeenCalled()
})
it('does not focus cached content after identity changes and cancels timers on unmount', () => {
  const scroll = vi.spyOn(Element.prototype, 'scrollIntoView')
  useAuthStore.getState().setAccess({ userId: 'other' })
  const view = render(<Target />)
  expect(scroll).not.toHaveBeenCalled()
  view.unmount()
  expect(vi.getTimerCount()).toBe(0)
})
it('renders Vietnamese friend notification copy and keeps the existing message action', () => {
  const common = { t: translate, pending: false, action: { mutate: vi.fn() }, lifecycle: {} }
  const view = render(
    <NotificationCard
      {...common}
      item={{
        id: 'notice',
        type: 'FriendRequest',
        referenceId: 'target',
        createdAt: '2026-11-16T00:00:00Z',
        text: 'An đã gửi cho bạn một lời mời kết bạn.',
      }}
    />,
  )
  expect(screen.getByRole('heading')).toHaveTextContent('Bạn có lời mời kết bạn mới')
  expect(screen.getByText('An đã gửi cho bạn một lời mời kết bạn.')).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Xem lời mời' })).toBeInTheDocument()
  view.rerender(
    <NotificationCard
      {...common}
      item={{
        id: 'notice',
        type: 'Message',
        referenceId: 'message',
        createdAt: '2026-11-16T00:00:00Z',
      }}
    />,
  )
  expect(screen.getByRole('button', { name: 'Xem nội dung' })).toBeInTheDocument()
})

it('opens a friend request from quick preview, marking only its notification read', async () => {
  vi.useRealTimers()
  const notification = {
    id: 'notice',
    type: 'FriendRequest',
    referenceId: 'target',
    createdAt: '2026-11-16T00:00:00Z',
    readAt: null,
  }
  vi.spyOn(notificationsApi, 'list').mockResolvedValue({ items: [notification], hasMore: false })
  vi.spyOn(notificationsApi, 'count').mockResolvedValue({ count: 1 })
  const read = vi.spyOn(notificationsApi, 'read').mockResolvedValue(undefined)
  const seen = vi.spyOn(friendsApi, 'markSeen').mockResolvedValue(undefined)
  function Destination() {
    const location = useLocation()
    return (
      <span data-testid="destination">
        {location.pathname}
        {location.search}
      </span>
    )
  }
  const view = render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <NotificationPreview />
        <Destination />
      </MemoryRouter>
    </QueryClientProvider>,
  )
  fireEvent.click(await screen.findByRole('button', { name: /Xem nhanh thông báo/ }))
  fireEvent.click(await screen.findByRole('button', { name: 'Xem lời mời' }))
  await screen.findByText('/friends/incoming?request=target')
  expect(read).toHaveBeenCalledWith('notice')
  expect(seen).not.toHaveBeenCalled()
  view.unmount()
  queryClient.clear()
})
