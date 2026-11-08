import { useQuery } from '@tanstack/react-query'
import { useAuthStore } from '@/stores/auth-store'
import { bcrFeatures } from '@/lib/bcr-features'
import { friendsApi } from './api'

export const friendUnreadKey = (userId) => ['workspace', 'friendUnread', userId]
export function useFriendUnread() {
  const userId = useAuthStore((state) => state.access?.userId)
  return useQuery({ queryKey: friendUnreadKey(userId), queryFn: friendsApi.unreadCount, enabled: !!userId && bcrFeatures.friendSeen })
}
