import { create } from 'zustand'
import { useAuthStore } from './auth-store'

const key = (userId) => `mechat-ui-toast:${userId}`
export const useUiFeedbackStore = create(() => ({ preferences: {}, notice: null }))
export function toastEnabled(userId) {
  if (!userId) return true
  const preferences = useUiFeedbackStore.getState().preferences
  if (Object.hasOwn(preferences, userId)) return preferences[userId]
  try {
    return localStorage.getItem(key(userId)) !== 'false'
  } catch {
    return true
  }
}
export function setToastEnabled(userId, enabled) {
  if (!userId) return
  try {
    localStorage.setItem(key(userId), String(enabled))
  } catch {
    /* In-memory fallback when storage is unavailable. */
  }
  useUiFeedbackStore.setState((state) => ({
    preferences: { ...state.preferences, [userId]: enabled },
    notice: null,
  }))
}
export function dismissToast(id) {
  useUiFeedbackStore.setState((state) => (state.notice?.id === id ? { notice: null } : {}))
}
export function showSuccess(message) {
  const userId = useAuthStore.getState().access?.userId
  if (userId && toastEnabled(userId))
    useUiFeedbackStore.setState({ notice: { id: crypto.randomUUID(), userId, message } })
}
