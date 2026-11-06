import { http } from '@/lib/http'

export const friendsApi = {
  list: async ({ signal }) => (await http.get('/friends', { signal })).data,
  requests: async ({ signal }) => (await http.get('/friends/requests', { signal })).data,
  blocks: async ({ signal }) => (await http.get('/blocks', { signal })).data,
  send: async (targetUserId) => (await http.post('/friends/requests', { targetUserId })).data,
  accept: async (id) => (await http.post(`/friends/requests/${id}/accept`)).data,
  reject: async (id) => (await http.post(`/friends/requests/${id}/reject`)).data,
  cancel: async (id) => (await http.post(`/friends/requests/${id}/cancel`)).data,
  unfriend: async (targetUserId) => (await http.delete(`/friends/${targetUserId}`)).data,
  block: async (targetUserId) => (await http.post('/blocks', { targetUserId })).data,
  unblock: async (targetUserId) => (await http.delete(`/blocks/${targetUserId}`)).data,
}
