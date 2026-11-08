import { act, fireEvent, render, renderHook, screen, waitFor } from '@testing-library/react'
import { QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router'
import { afterEach, beforeEach, expect, it, vi } from 'vitest'
import { Bell, UsersRound } from 'lucide-react'
import { queryClient } from '@/lib/query-client'
import { useAuthStore } from '@/stores/auth-store'
import { NotificationsPage } from '@/features/notifications/NotificationsPage'
import { NavigationLinks } from '@/components/layout/NavigationLinks'
import { notificationsApi } from '@/features/notifications/api'
import { friendsApi } from '@/features/friends/api'
import { bcrFeatures } from '@/lib/bcr-features'
import { useRequestSeen } from '@/features/friends/use-request-seen'

const { queues } = vi.hoisted(() => ({ queues: [] }))
vi.mock('@/features/friends/seen-queue', () => ({ createSeenQueue: options => {
  const queue = { options, register: vi.fn(), stop: vi.fn(), retry: vi.fn() }
  queues.push(queue)
  return queue
} }))

vi.mock('@/lib/bcr-features', () => ({ bcrFeatures: { notifications: true, friendSeen: true } }))
vi.mock('@/components/layout/ApplicationLayout', () => ({ ApplicationLayout: ({ children, actions }) => <main>{actions}{children}</main> }))
vi.mock('@/features/messages/api', () => ({ messagesApi: { summary: async () => ({ requestCount: 99, conversations: [] }) } }))
vi.mock('@/features/notifications/api', () => ({ notificationsApi: { list: vi.fn(), count: vi.fn(), remove: vi.fn(), restore: vi.fn(), deleteRead: vi.fn() } }))
vi.mock('@/features/friends/api', () => ({ friendsApi: { unreadCount: vi.fn() } }))
let items
const mount = (child = <NotificationsPage />) => render(<QueryClientProvider client={queryClient}><MemoryRouter>{child}</MemoryRouter></QueryClientProvider>)
beforeEach(() => {
  bcrFeatures.notifications = true; bcrFeatures.friendSeen = true
  queues.length = 0
  useAuthStore.getState().setAccess({ userId: 'actor', accessToken: 'test-only' })
  items = [{ id: 'n1', type: 'AdminWarning', createdAt: '2026-11-08T10:00:00Z', readAt: null, text: 'Synthetic notice' }]
  notificationsApi.list.mockImplementation(async () => ({ items, hasMore: false }))
  notificationsApi.count.mockResolvedValue({ count: 1 })
  notificationsApi.remove.mockReset(); friendsApi.unreadCount.mockResolvedValue({ unreadCount: 3 })
})
afterEach(() => { queryClient.clear(); useAuthStore.getState().clear(); vi.clearAllMocks() })
it('keeps Vietnamese labels intact and hides new controls/count fetches while switches are off', async () => {
  bcrFeatures.notifications = false; bcrFeatures.friendSeen = false
  mount(<><NotificationsPage /><NavigationLinks items={[{ to: '/friends', label: 'Bạn bè', Icon: UsersRound }]} /></>)
  await screen.findByText('Synthetic notice')
  expect(screen.queryByRole('button', { name: 'Thùng rác' })).not.toBeInTheDocument()
  expect(screen.queryByRole('button', { name: 'Xóa', exact: true })).not.toBeInTheDocument()
  expect(friendsApi.unreadCount).not.toHaveBeenCalled()
})
it('never removes a notification or updates count before server confirmation', async () => {
  let resolve
  notificationsApi.remove.mockImplementation(() => new Promise(done => { resolve = () => { items = []; done() } }))
  mount(); fireEvent.click(await screen.findByRole('button', { name: 'Xóa', exact: true }))
  await waitFor(() => expect(notificationsApi.remove).toHaveBeenCalledOnce())
  expect(screen.getByText('Synthetic notice')).toBeInTheDocument()
  expect(notificationsApi.count).toHaveBeenCalledTimes(1)
  await act(async () => resolve())
  await waitFor(() => expect(screen.queryByText('Synthetic notice')).not.toBeInTheDocument())
})
it('maps safe backend codes to Vietnamese and keeps failed items visible', async () => {
  notificationsApi.remove.mockRejectedValue({ response: { status: 404, data: { code: 'NOTIFICATION_UNAVAILABLE', detail: 'must-never-render' } } })
  mount(); fireEvent.click(await screen.findByRole('button', { name: 'Xóa', exact: true }))
  expect(await screen.findByRole('alert')).toHaveTextContent('Thông báo không còn khả dụng hoặc bạn không có quyền xem.')
  expect(screen.getByText('Synthetic notice')).toBeInTheDocument()
  expect(screen.queryByText('must-never-render')).not.toBeInTheDocument()
})
it('uses a dedicated friend server count independently of message requests and pending rows', async () => {
  mount(<NavigationLinks items={[{ to: '/friends', label: 'Bạn bè', Icon: UsersRound }, { to: '/notifications', label: 'Thông báo', Icon: Bell }]} />)
  const friends = await screen.findByRole('link', { name: /3 mục chưa đọc Bạn bè/ })
  expect(friends).toHaveTextContent('3'); expect(friends).not.toHaveTextContent('99')
})
it('renders source warning as text and does not interpret markup', async () => {
  items[0].text = '<img src=x onerror="alert(1)">'
  const { container } = mount()
  await screen.findByText('<img src=x onerror="alert(1)">')
  expect(container.querySelector('img')).toBeNull()
})
it('does not expose previous-account notification cache after identity switch', async () => {
  mount(); await screen.findByText('Synthetic notice')
  notificationsApi.list.mockImplementation(() => new Promise(() => {}))
  await act(async () => useAuthStore.getState().setAccess({ userId: 'another-account', accessToken: 'test-only' }))
  expect(screen.queryByText('Synthetic notice')).not.toBeInTheDocument()
})

it('hides seen-queue error/retry when the incoming view is inactive', () => {
  const { result, rerender } = renderHook(({ enabled }) => useRequestSeen(enabled), { initialProps: { enabled: true } })
  act(() => queues[0].options.failed(new Error('test-only')))
  expect(result.current.error).toBeTruthy()
  rerender({ enabled: false })
  expect(result.current.error).toBeNull(); expect(queues[0].stop).toHaveBeenCalled()
})

it('stops pending seen work immediately on identity loss', () => {
  renderHook(() => useRequestSeen(true))
  act(() => useAuthStore.getState().clear())
  expect(queues[0].stop).toHaveBeenCalled()
})
