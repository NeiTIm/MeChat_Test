import { renderHook } from '@testing-library/react'
import { afterEach, expect, it, vi } from 'vitest'
import { useBlobUrl } from '@/features/files/AttachmentPreview'

afterEach(() => vi.unstubAllGlobals())
it('handles unloaded blobs and revokes an image immediately after permission data disappears', () => {
  const create = vi.fn(() => 'blob:test-only')
  const revoke = vi.fn()
  vi.stubGlobal('URL', { createObjectURL: create, revokeObjectURL: revoke })
  const { result, rerender, unmount } = renderHook(({ blob }) => useBlobUrl(blob), { initialProps: { blob: undefined } })
  expect(result.current).toBeNull()
  const blob = new Blob(['test-only'], { type: 'text/plain' })
  rerender({ blob })
  expect(result.current).toBe('blob:test-only')
  rerender({ blob: undefined })
  expect(result.current).toBeNull()
  expect(revoke).toHaveBeenCalledWith('blob:test-only')
  unmount()
})
