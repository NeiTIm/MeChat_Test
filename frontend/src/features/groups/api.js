import { http } from '@/lib/http'

export const groupsApi = {
  create: async (input) => (await http.post('/groups', input)).data,
  update: async (id, input) => (await http.patch(`/groups/${id}`, input)).data,
  invite: async (id, inviteeUserId) =>
    (await http.post(`/groups/${id}/invitations`, { inviteeUserId })).data,
  invitations: async ({ signal }) => (await http.get('/group-invitations', { signal })).data,
  accept: async (id) => (await http.post(`/group-invitations/${id}/accept`)).data,
  reject: async (id) => (await http.post(`/group-invitations/${id}/reject`)).data,
  leave: async (id) => (await http.post(`/groups/${id}/leave`)).data,
  role: async (id, userId, role) =>
    (await http.patch(`/groups/${id}/members/${userId}/role`, { role })).data,
  remove: async (id, userId) => (await http.delete(`/groups/${id}/members/${userId}`)).data,
  transfer: async (id, newOwnerUserId) =>
    (await http.post(`/groups/${id}/transfer-owner`, { newOwnerUserId })).data,
  dissolve: async (id) => (await http.post(`/groups/${id}/dissolve`, { confirm: true })).data,
  pins: async (id, signal) => (await http.get(`/groups/${id}/pins`, { signal })).data,
  pinPage: async (id, beforeSequence, signal) =>
    (await http.get(`/groups/${id}/pins/page`, { params: { beforeSequence, limit: 30 }, signal }))
      .data,
  pin: async (id, messageId) => (await http.post(`/groups/${id}/pins`, { messageId })).data,
  unpin: async (id, messageId) => (await http.delete(`/groups/${id}/pins/${messageId}`)).data,
  links: async (id, signal) => (await http.get(`/groups/${id}/invite-links`, { signal })).data,
  createLink: async (id, expiresAt) =>
    (await http.post(`/groups/${id}/invite-links`, { expiresAt })).data,
  revokeLink: async (id, linkId) =>
    (await http.delete(`/groups/${id}/invite-links/${linkId}`)).data,
  resolveLink: async (token) => (await http.post('/group-invite-links/resolve', { token })).data,
  joinLink: async (token) =>
    (await http.post('/group-invite-links/join', { token, confirm: true })).data,
  hide: async (id) => (await http.delete(`/groups/${id}/for-me`)).data,
  list: async ({ signal }) => (await http.get('/groups', { signal })).data,
  get: async (id, signal) => (await http.get(`/groups/${id}`, { signal })).data,
  history: async (id, beforeSequence, signal) =>
    (await http.get(`/groups/${id}/messages`, { params: { beforeSequence, limit: 30 }, signal }))
      .data,
  members: async (id, signal) => (await http.get(`/groups/${id}/members`, { signal })).data,
  send: async (id, input) => (await http.post(`/groups/${id}/messages`, input)).data,
}
