import { act, renderHook, waitFor } from '@testing-library/react'
import { afterEach, expect, it, vi } from 'vitest'
import { useAttachmentDraft } from '@/features/files/use-attachment-draft'
import { filesApi } from '@/features/files/api'

afterEach(() => vi.restoreAllMocks())
const sample = () => new File(['Dữ liệu test tổng hợp.'], 'nhat-ky.txt', { type: 'text/plain' })
it('reports progress, aborts removal, and ignores a late completion', async () => {
  let signal, finish
  vi.spyOn(filesApi, 'upload').mockImplementation((_file, purpose, requestSignal, progress) => {
    expect(purpose).toBe('Message'); signal = requestSignal; progress(47)
    return new Promise((resolve) => { finish = resolve })
  })
  const { result } = renderHook(useAttachmentDraft)
  act(() => result.current.add([sample()]))
  expect(result.current.items[0].progress).toBe(47)
  act(() => result.current.remove(result.current.items[0].key))
  expect(signal.aborted).toBe(true)
  await act(async () => finish({ id: 'cancelled' }))
  expect(result.current.items).toEqual([])
  expect(result.current.readyIds).toEqual([])
})
it('retries a failed upload without duplicating draft entries and aborts on unmount', async () => {
  const upload = vi.spyOn(filesApi, 'upload').mockRejectedValueOnce(new Error('test network failure')).mockResolvedValueOnce({ id: 'ready' })
  const { result, unmount } = renderHook(useAttachmentDraft)
  act(() => result.current.add([sample()]))
  await waitFor(() => expect(result.current.items[0].state).toBe('error'))
  await act(() => result.current.retry(result.current.items[0]))
  expect(result.current.items).toHaveLength(1)
  expect(result.current.readyIds).toEqual(['ready'])
  expect(upload).toHaveBeenCalledTimes(2)
  let pendingSignal
  upload.mockImplementation((_file, _purpose, signal) => { pendingSignal = signal; return new Promise(() => {}) })
  act(() => result.current.add([sample()]))
  unmount()
  expect(pendingSignal.aborted).toBe(true)
})
