import { act, render, screen, fireEvent } from '@testing-library/react'
import { afterEach, beforeEach, expect, it, vi } from 'vitest'
import { ToastViewport } from '@/components/ToastViewport'
import { useAuthStore } from '@/stores/auth-store'
import { setToastEnabled, toastEnabled, showSuccess, useUiFeedbackStore } from '@/stores/ui-feedback-store'

beforeEach(() => {
  vi.useFakeTimers()
  localStorage.clear()
  useUiFeedbackStore.setState({ preferences: {}, notice: null })
  useAuthStore.setState({ access: { userId: 'test-a' } })
})
afterEach(() => { vi.useRealTimers(); useAuthStore.setState({ access: null }) })
it('dismisses success after 2 seconds, supports pause and an immediate close button', () => {
  render(<ToastViewport />)
  act(() => showSuccess('Đã lưu hồ sơ.'))
  expect(screen.getByRole('status')).toHaveTextContent('Đã lưu hồ sơ.')
  act(() => vi.advanceTimersByTime(1999))
  expect(screen.getByRole('status')).toBeVisible()
  fireEvent.focus(screen.getByRole('button'))
  act(() => vi.advanceTimersByTime(5000))
  expect(screen.getByRole('status')).toBeVisible()
  fireEvent.click(screen.getByRole('button', { name: 'Đóng thông báo nhanh' }))
  expect(screen.queryByRole('status')).toBeNull()
  act(() => showSuccess('Đã lưu lần nữa.'))
  act(() => vi.advanceTimersByTime(2000))
  expect(screen.queryByRole('status')).toBeNull()
})
it('keeps a local account preference across reload without sharing it with another identity', () => {
  setToastEnabled('test-a', false)
  expect(toastEnabled('test-a')).toBe(false)
  useUiFeedbackStore.setState({ preferences: {} })
  expect(toastEnabled('test-a')).toBe(false)
  expect(toastEnabled('test-b')).toBe(true)
  showSuccess('Không hiện toast khi đã tắt.')
  expect(useUiFeedbackStore.getState().notice).toBeNull()
  expect(Object.values(localStorage)).not.toContain('test-only-token')
})
it('clears an old identity toast immediately on logout or account switch', () => {
  render(<ToastViewport />)
  act(() => showSuccess('Phản hồi của A.'))
  act(() => useAuthStore.setState({ access: { userId: 'test-b' } }))
  expect(screen.queryByRole('status')).toBeNull()
  expect(useUiFeedbackStore.getState().notice).toBeNull()
})
