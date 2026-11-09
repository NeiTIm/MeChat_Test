import { http } from '@/lib/http'

export const filesApi = {
  upload: async (file, purpose, signal, onProgress) => {
    const form = new FormData()
    form.append('file', file)
    form.append('purpose', purpose)
    return (
      await http.post('/files', form, {
        signal,
        onUploadProgress: (event) =>
          onProgress?.(event.total ? Math.round((event.loaded / event.total) * 100) : null),
      })
    ).data
  },
  preview: async (id, signal) =>
    (await http.get(`/files/${id}/preview`, { signal, responseType: 'blob' })).data,
  download: async (id, signal) =>
    (await http.get(`/files/${id}/download`, { signal, responseType: 'blob' })).data,
  avatar: async (storedFileId) => (await http.post('/users/me/avatar', { storedFileId })).data,
}
