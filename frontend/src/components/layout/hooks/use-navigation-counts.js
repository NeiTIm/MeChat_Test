import { useQuery } from '@tanstack/react-query'
import { useAuthStore } from '@/stores/auth-store'
import { messagesApi } from '@/features/messages/api'
import { notificationsApi } from '@/features/notifications/api'
import { useFriendUnread } from '@/features/friends/use-friend-unread'
export function useNavigationCounts() {
  const userId = useAuthStore((state) => state.access?.userId)
  const friends = useFriendUnread()
  const summary = useQuery({
    queryKey: ['workspace', 'unreadSummary'],
    queryFn: messagesApi.summary,
  })
  const notifications = useQuery({
    queryKey: ['workspace', 'notificationCount', userId],
    queryFn: notificationsApi.count,
  })
  // These entries are the complete server projection, not the rendered inbox rows.
  const total = (type) =>
    summary.data?.conversations
      .filter((entry) => entry.type === type)
      .reduce((sum, entry) => sum + entry.count, 0)
  const privateCount = total('Private')
  const counts = {
    '/chat': privateCount === undefined ? undefined : privateCount + summary.data.requestCount,
    '/groups': total('Group'),
    '/notifications': notifications.data?.count,
    '/friends': friends.data?.unreadCount,
  }

  return counts
}
