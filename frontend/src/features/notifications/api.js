import { http } from '@/lib/http'
export const notificationsApi = {
  list: async (cursor, signal) =>
    (
      await http.get('/notifications', {
        params: { before: cursor?.before, beforeId: cursor?.beforeId, limit: 30 },
        signal,
      })
    ).data,
  count: async ({ signal }) => (await http.get('/notifications/unread-count', { signal })).data,
  read: async (id) => (await http.patch(`/notifications/${id}/read`)).data,
  readAll: async () => (await http.post('/notifications/read-all')).data,
  trash: async (cursor, signal) =>
    (
      await http.get('/notifications/trash', {
        params: { before: cursor?.before, beforeId: cursor?.beforeId, limit: 30 },
        signal,
      })
    ).data,
  remove: async (id, signal) => (await http.delete(`/notifications/${id}`, { signal })).data,
  deleteRead: async (signal) => (await http.delete('/notifications/read', { signal })).data,
  restore: async (id, signal) =>
    (await http.post(`/notifications/${id}/restore`, null, { signal })).data,
  settings: async (id, signal) =>
    (await http.get(`/conversations/${id}/settings`, { signal })).data,
  mute: async (id, mutedUntil) =>
    (await http.patch(`/conversations/${id}/settings`, { mutedUntil })).data,
}
