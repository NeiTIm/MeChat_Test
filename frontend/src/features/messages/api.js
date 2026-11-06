import { http } from '@/lib/http'

export const messagesApi = {
  get: async (id, signal) => (await http.get(`/messages/${id}`, { signal })).data,
  context: async (id, signal) => (await http.get(`/messages/${id}/context`, { signal })).data,
  edit: async (id, content) => (await http.patch(`/messages/${id}`, { content })).data,
  history: async (id, signal) => (await http.get(`/messages/${id}/edit-history`, { signal })).data,
  recall: async (id) => (await http.post(`/messages/${id}/recall`)).data,
  deleteForMe: async (id) => (await http.delete(`/messages/${id}/for-me`)).data,
  react: async (id, emoji) => (await http.put(`/messages/${id}/reaction`, { emoji })).data,
  removeReaction: async (id) => (await http.delete(`/messages/${id}/reaction`)).data,
  forward: async (id, destinationConversationId, clientMessageId) => (await http.post(`/messages/${id}/forward`, { destinationConversationId, clientMessageId })).data,
  receipts: async (id, signal) => (await http.get(`/messages/${id}/receipts`, { signal })).data,
  acknowledge: async (id, messageIds) => (await http.post(`/conversations/${id}/delivery-acks`, { messageIds })).data,
  read: async (id, lastReadSequence) => (await http.put(`/conversations/${id}/read-cursor`, { lastReadSequence })).data,
  unread: async (id, signal) => (await http.get(`/conversations/${id}/unread`, { signal })).data,
  summary: async ({ signal }) => (await http.get('/unread', { signal })).data,
  search: async (id, q, beforeSequence, signal) => (await http.get(`/conversations/${id}/messages/search`, { params: { q, beforeSequence, limit: 30 }, signal })).data,
  presence: async (id, signal) => (await http.get(`/users/${id}/presence`, { signal })).data,
}
export const requestsApi = {
  list: async ({ signal }) => (await http.get('/message-requests', { signal })).data,
  count: async ({ signal }) => (await http.get('/message-requests/unread-count', { signal })).data,
  accept: async (id) => (await http.post(`/message-requests/${id}/accept`)).data,
  reject: async (id) => (await http.post(`/message-requests/${id}/reject`)).data,
  block: async (id) => (await http.post(`/message-requests/${id}/block`)).data,
  read: async (id, lastReadSequence) => (await http.put(`/message-requests/${id}/read-cursor`, { lastReadSequence })).data,
  renew: async (id) => (await http.post(`/conversations/${id}/message-request`, { confirm: true })).data,
}
