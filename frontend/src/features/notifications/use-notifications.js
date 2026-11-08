import { useState } from 'react'
import { useInfiniteQuery, useMutation, useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router'
import { queryClient } from '@/lib/query-client'
import { messagesApi } from '@/features/messages/api'
import { notificationsApi } from './api'

export function useNotifications(enabled = true) {
  const feed = useInfiniteQuery({ queryKey: ['workspace', 'notifications'], queryFn: ({ pageParam, signal }) => notificationsApi.list(pageParam, signal), initialPageParam: undefined, getNextPageParam: (page) => page.hasMore ? { before: page.nextCursor, beforeId: page.nextCursorId } : undefined, enabled })
  const count = useQuery({ queryKey: ['workspace', 'notificationCount'], queryFn: notificationsApi.count })
  const items = [...new Map((feed.data?.pages || []).flatMap((page) => page.items).map((item) => [item.id, item])).values()]
  return { feed, count, items }
}
export function useNotificationActions(onNavigate) {
  const navigate = useNavigate()
  const [notice, setNotice] = useState(false)
  const action = useMutation({ mutationFn: async ({ notification, all, open }) => {
    setNotice(false)
    let target
    if (open && ['Message', 'GroupMessage', 'MessageRequest'].includes(notification.type) && notification.referenceId) {
      const message = await messagesApi.get(notification.referenceId)
      target = `${notification.type === 'GroupMessage' ? '/groups' : notification.type === 'MessageRequest' ? '/message-requests' : '/chat'}/${message.conversationId}`
    }
    if (all) await notificationsApi.readAll()
    else await notificationsApi.read(notification.id)
    await queryClient.invalidateQueries({ queryKey: ['workspace'] })
    if (target) { onNavigate?.(); navigate(target) }
    else setNotice(true)
  } })
  return { action, notice }
}
