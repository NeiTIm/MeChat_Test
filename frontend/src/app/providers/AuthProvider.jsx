import { useEffect } from 'react'
import { refreshSession, clearSession, configureSessionLoss } from '@/lib/http'
import { queryClient } from '@/lib/query-client'
import { useAuthStore } from '@/stores/auth-store'

let channel
export function broadcastLogout() {
  channel?.postMessage({ type: 'logout' })
}
export function retryRecovery() {
  useAuthStore.setState({ status: 'loading', recoveryError: null })
  return refreshSession().catch((error) => {
    if (error.response?.status !== 401) useAuthStore.getState().unavailable(error)
  })
}
export function AuthProvider({ children }) {
  useEffect(() => {
    configureSessionLoss(() => {
      queryClient.cancelQueries()
      queryClient.clear()
    })
    retryRecovery()
    if ('BroadcastChannel' in window) {
      channel = new BroadcastChannel('mechat-session')
      channel.onmessage = (event) => {
        if (event.data?.type === 'logout') clearSession()
      }
    }
    return () => {
      channel?.close()
      channel = null
    }
  }, [])
  return children
}
