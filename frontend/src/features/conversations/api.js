import { http } from '@/lib/http'

export const conversationsApi = {
  open: async (targetUserId) => (await http.post('/conversations/private', { targetUserId })).data,
  hide: async (id) => (await http.delete(`/conversations/${id}/for-me`)).data,
  list: async ({ signal }) => (await http.get('/conversations', { signal })).data,
  get: async (id, signal) => (await http.get(`/conversations/${id}`, { signal })).data,
  history: async (id, beforeSequence, signal) =>
    (
      await http.get(`/conversations/${id}/messages`, {
        params: { beforeSequence, limit: 30 },
        signal,
      })
    ).data,
  send: async (id, input) => (await http.post(`/conversations/${id}/messages`, input)).data,
}
