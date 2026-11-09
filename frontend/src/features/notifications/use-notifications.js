import { useEffect, useRef, useState } from 'react'
import { useInfiniteQuery, useMutation, useQuery } from '@tanstack/react-query'
import { useLocation, useNavigate } from 'react-router'
import { queryClient } from '@/lib/query-client'
import { messagesApi } from '@/features/messages/api'
import { notificationsApi } from './api'
import { useAuthStore } from '@/stores/auth-store'
import { bcrFeatures } from '@/lib/bcr-features'
import { notificationMessageTarget } from './message-target'

export function notificationNextPage(page) {
  // An empty visible page can still carry a raw scan cursor. Preserve exact timestamp precision.
  return page.hasMore && page.nextCursor && page.nextCursorId
    ? { before: page.nextCursor, beforeId: page.nextCursorId }
    : undefined
}
export function useNotifications(enabled = true, trash = false) {
  const userId = useAuthStore((state) => state.access?.userId)
  const feed = useInfiniteQuery({
    queryKey: ['workspace', 'notifications', userId, trash ? 'trash' : 'active'],
    queryFn: ({ pageParam, signal }) =>
      (trash ? notificationsApi.trash : notificationsApi.list)(pageParam, signal),
    initialPageParam: undefined,
    getNextPageParam: notificationNextPage,
    enabled: enabled && !!userId && (!trash || bcrFeatures.notifications),
  })
  const count = useQuery({
    queryKey: ['workspace', 'notificationCount', userId],
    queryFn: notificationsApi.count,
    enabled: !!userId,
  })
  const items = [
    ...new Map(
      (feed.data?.pages || []).flatMap((page) => page.items).map((item) => [item.id, item]),
    ).values(),
  ]
  return { feed, count, items }
}
export function useNotificationActions(onNavigate) {
  const navigate = useNavigate()
  const location = useLocation()
  const userId = useAuthStore((state) => state.access?.userId)
  const active = useRef(null)
  useEffect(() => () => active.current?.abort(), [location.key])
  useEffect(() => {
    const unsubscribe = useAuthStore.subscribe((state, old) => {
      if (state.access?.userId !== old.access?.userId) active.current?.abort()
    })
    return () => {
      unsubscribe()
      active.current?.abort()
    }
  }, [])
  const [notice, setNotice] = useState(false)
  const action = useMutation({
    mutationFn: async ({ notification, all, open }) => {
      setNotice(false)
      active.current?.abort()
      const controller = new AbortController()
      active.current = controller
      let target
      if (
        open &&
        ['Message', 'GroupMessage', 'MessageRequest'].includes(notification.type) &&
        notification.referenceId
      ) {
        const message = await messagesApi.get(notification.referenceId, controller.signal)
        if (controller.signal.aborted || useAuthStore.getState().access?.userId !== userId) return
        target = notificationMessageTarget(notification, message)
      }
      if (all) await notificationsApi.readAll()
      else await notificationsApi.read(notification.id)
      if (controller.signal.aborted || useAuthStore.getState().access?.userId !== userId) return
      await queryClient.invalidateQueries(
        {
          queryKey: ['workspace'],
          predicate: (q) =>
            ['notifications', 'notificationCount', 'unreadSummary'].includes(q.queryKey[1]),
        },
        { cancelRefetch: false },
      )
      if (controller.signal.aborted || useAuthStore.getState().access?.userId !== userId) return
      if (target) {
        onNavigate?.()
        navigate(target)
      } else setNotice(true)
    },
  })
  return { action, notice }
}

export function useNotificationLifecycle() {
  const userId = useAuthStore((state) => state.access?.userId)
  const requests = useRef(new Set())
  useEffect(() => {
    const cancel = () => {
      for (const controller of requests.current) controller.abort()
      requests.current.clear()
    }
    const unsubscribe = useAuthStore.subscribe((state, old) => {
      if (state.access?.userId !== old.access?.userId) cancel()
    })
    return () => {
      unsubscribe()
      cancel()
    }
  }, [userId])
  return useMutation({
    mutationFn: async ({ method, id }) => {
      if (
        !userId ||
        !bcrFeatures.notifications ||
        useAuthStore.getState().access?.userId !== userId
      )
        throw new Error('SESSION_ENDED')
      const controller = new AbortController()
      requests.current.add(controller)
      try {
        if (method === 'deleteRead') await notificationsApi.deleteRead(controller.signal)
        else if (method === 'restore') await notificationsApi.restore(id, controller.signal)
        else await notificationsApi.remove(id, controller.signal)
      } finally {
        requests.current.delete(controller)
      }
      // Do not populate a later account's cache with this mutation's result.
      if (useAuthStore.getState().access?.userId === userId)
        await queryClient.invalidateQueries({ queryKey: ['workspace'] })
    },
  })
}
