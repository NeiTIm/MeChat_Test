import axios from 'axios'
import { useAuthStore } from '@/stores/auth-store'

export const http = axios.create({ baseURL: '/api', withCredentials: true, timeout: 20_000 })
let refreshPromise
let generation = 0
let onSessionLost = () => {}
function validAccess(access) {
  return access && ['accessToken', 'userId', 'username'].every((key) => typeof access[key] === 'string' && access[key].length > 0) && Number.isFinite(new Date(access.expiresAt).getTime())
}
export function configureSessionLoss(callback) { onSessionLost = callback }
export function adoptSession(access) {
  if (!validAccess(access)) { clearSession(); throw new Error('INVALID_SESSION_RESPONSE') }
  generation += 1
  onSessionLost()
  useAuthStore.getState().setAccess(access)
}

export function csrfToken() {
  const value = document.cookie.split('; ').find((part) => part.startsWith('webchat_csrf='))
  return value ? decodeURIComponent(value.substring('webchat_csrf='.length)) : null
}

export function clearSession() {
  generation += 1
  useAuthStore.getState().clear()
  onSessionLost()
}

export async function refreshSession() {
  if (!refreshPromise) {
    const started = generation
    const rotate = async () => {
      const csrf = csrfToken()
      if (!csrf) { clearSession(); return null }
      try {
        const { data } = await http.post('/auth/refresh', null, { skipAuth: true, headers: { 'X-CSRF-Token': csrf } })
        if (generation !== started) return null
        if (!validAccess(data)) { clearSession(); throw new Error('INVALID_SESSION_RESPONSE') }
        if (useAuthStore.getState().access?.userId && useAuthStore.getState().access.userId !== data.userId) onSessionLost()
        useAuthStore.getState().setAccess(data)
        return data
      } catch (error) {
        if (generation === started && error.response?.status === 401) clearSession()
        throw error
      }
    }
    // Cookies are shared across tabs. Read CSRF inside the lock after previous rotation.
    refreshPromise = (navigator.locks?.request
      ? navigator.locks.request('mechat-refresh', rotate)
      : rotate()).finally(() => { refreshPromise = null })
  }
  return refreshPromise
}

export async function accessToken() {
  let access = useAuthStore.getState().access
  if (!access || new Date(access.expiresAt).getTime() - Date.now() < 60_000) access = await refreshSession()
  if (!access) throw new Error('SESSION_ENDED')
  return access.accessToken
}

http.interceptors.request.use((config) => {
  if (['/auth/refresh', '/auth/logout'].includes(config.url)) {
    const csrf = csrfToken()
    if (csrf) config.headers['X-CSRF-Token'] = csrf
  }
  if (!config.skipAuth) {
    const session = useAuthStore.getState().access
    const token = session?.accessToken
    config.actorUserId ??= session?.userId
    if (token) config.headers.Authorization = `Bearer ${token}`
  }
  return config
})
http.interceptors.response.use((response) => response, async (error) => {
  const config = error.config
  if (error.response?.status !== 401 || !config || config.skipAuth || config.retried) throw error
  config.retried = true
  const access = await refreshSession()
  if (!access) throw error
  if (config.actorUserId && config.actorUserId !== access.userId) { clearSession(); throw error }
  config.headers.Authorization = `Bearer ${access.accessToken}`
  try { return await http(config) }
  catch (retryError) {
    if (retryError.response?.status === 401) clearSession()
    throw retryError
  }
})
