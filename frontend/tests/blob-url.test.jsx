import { fireEvent, render, renderHook, screen } from '@testing-library/react'
import { afterEach, expect, it, vi } from 'vitest'
import { useBlobUrl } from '@/features/files/AttachmentPreview'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { Avatar } from '@/components/Avatar'

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
it('falls back to initials when an authorized avatar cannot be decoded', async () => {
  vi.stubGlobal('URL', { createObjectURL: vi.fn(() => 'blob:synthetic-broken-avatar'), revokeObjectURL: vi.fn() })
  const client = new QueryClient({ defaultOptions: { queries: { staleTime: Infinity } } })
  client.setQueryData(['workspace', 'file', 'broken-avatar'], new Blob(['invalid image test'], { type: 'image/png' }))
  const view = render(<QueryClientProvider client={client}><Avatar fileId="broken-avatar" name="Người Kiểm Thử" /></QueryClientProvider>)
  const image = await screen.findByAltText('')
  fireEvent.error(image)
  expect(view.container.querySelector('img')).toBeNull()
  expect(screen.getByText('NK')).toBeInTheDocument()
  view.unmount()
  expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:synthetic-broken-avatar')
  client.clear()
})
