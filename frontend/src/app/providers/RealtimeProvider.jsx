import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react'
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { useAuthStore } from '@/stores/auth-store'
import { accessToken, clearSession } from '@/lib/http'
import { queryClient } from '@/lib/query-client'

export const REALTIME_EVENTS = [
  'MessageCreated', 'MessageEdited', 'MessageRecalled', 'MessageDeletedForMe',
  'MessageReactionChanged', 'MessageDelivered', 'MessageSeen', 'ConversationHidden',
  'TypingChanged', 'PresenceChanged', 'FriendshipChanged', 'PermissionsChanged',
  'MessageRequestChanged', 'GroupChanged', 'GroupInvitationChanged',
  'NotificationCreated', 'NotificationRead', 'SessionRevoked',
]
const RealtimeContext = createContext({ status: 'disconnected', typing: [], invoke: async () => false })
export function useRealtime() { return useContext(RealtimeContext) }

export function RealtimeProvider({ children }) {
  const userId = useAuthStore((state) => state.access?.userId)
  const [status, setStatus] = useState('disconnected')
  const [typing, setTyping] = useState([])
  const connectionRef = useRef(null)
  const invoke = useCallback(async (method, id) => {
    if (!['JoinConversation', 'LeaveConversation', 'StartTyping', 'StopTyping'].includes(method) || connectionRef.current?.state !== 'Connected') return false
    try { await connectionRef.current.invoke(method, id); return true } catch { return false }
  }, [])
  useEffect(() => {
    if (!userId) return
    let disposed = false
    let startTimer
    const seen = new Set()
    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/chat', { accessTokenFactory: accessToken })
      .configureLogging(LogLevel.None)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build()
    connectionRef.current = connection
    const sync = () => queryClient.invalidateQueries({ queryKey: ['workspace'] })
    for (const event of REALTIME_EVENTS) {
      connection.on(event, (payload) => {
        if (disposed) return
        if (payload?.eventId) {
          if (seen.has(payload.eventId)) return
          seen.add(payload.eventId)
          if (seen.size > 500) seen.delete(seen.values().next().value)
        }
        if (event === 'SessionRevoked') { clearSession(); return }
        if (event === 'TypingChanged') {
          setTyping((current) => [...current.filter((item) => item.conversationId !== payload.conversationId || item.actorId !== payload.actorId), ...(payload.isTyping ? [{ ...payload, recipientUserId: userId, expires: Math.min(Date.now() + 5000, new Date(payload.expiresAt).getTime()) }] : [])])
          return
        }
        if (['PermissionsChanged', 'GroupChanged', 'ConversationHidden', 'MessageDeletedForMe', 'MessageRecalled'].includes(event)) {
          setTyping([])
          // Reset first: do not leave old message content visible while permissions are checked.
          queryClient.resetQueries({ queryKey: ['workspace'] })
        } else if (event !== 'TypingChanged') sync()
      })
    }
    async function start() {
      if (disposed) return
      setStatus('connecting')
      try {
        await connection.start()
        if (disposed) { await connection.stop(); return }
        setStatus('connected')
        sync()
      } catch {
        if (disposed) return
        setStatus('disconnected')
        if (useAuthStore.getState().access?.userId === userId) startTimer = setTimeout(start, 15000)
      }
    }
    connection.onreconnecting(() => { if (!disposed) setStatus('reconnecting') })
    connection.onreconnected(() => { if (!disposed) { setStatus('connected'); sync() } })
    connection.onclose(() => {
      if (!disposed) { setStatus('disconnected'); startTimer = setTimeout(start, 15000) }
    })
    const foregroundSync = () => { if (document.visibilityState === 'visible' && navigator.onLine) sync() }
    const interval = setInterval(foregroundSync, 60000)
    const typingInterval = setInterval(() => setTyping((items) => {
      const remaining = items.filter((item) => item.expires > Date.now())
      return remaining.length === items.length ? items : remaining
    }), 1000)
    window.addEventListener('online', foregroundSync)
    document.addEventListener('visibilitychange', foregroundSync)
    start()
    return () => {
      disposed = true
      clearTimeout(startTimer)
      clearInterval(interval)
      clearInterval(typingInterval)
      window.removeEventListener('online', foregroundSync)
      document.removeEventListener('visibilitychange', foregroundSync)
      connection.stop()
      if (connectionRef.current === connection) connectionRef.current = null
    }
  }, [userId])
  const value = useMemo(() => ({ status: userId ? status : 'disconnected', typing: typing.filter((item) => item.recipientUserId === userId), invoke }), [status, userId, typing, invoke])
  return <RealtimeContext.Provider value={value}>{children}</RealtimeContext.Provider>
}
