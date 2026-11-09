import { http } from '@/lib/http'

export const usersApi = {
  search: async (query, cursor, signal) =>
    (await http.get('/users/search', { params: { query, cursor, limit: 30 }, signal })).data,
  me: async ({ signal } = {}) => (await http.get('/users/me', { signal })).data,
  profile: async (id, signal) => (await http.get(`/users/${id}`, { signal })).data,
  update: async (input) => (await http.patch('/users/me', input)).data,
  changeUsername: async (input) => (await http.patch('/users/me/username', input)).data,
  changeEmail: async (input) => (await http.post('/users/me/email-change', input)).data,
  confirmEmail: async (input) => (await http.post('/users/me/confirm-email-change', input)).data,
  privacy: async ({ signal }) => (await http.get('/users/me/privacy', { signal })).data,
  updatePrivacy: async (input) => (await http.patch('/users/me/privacy', input)).data,
}
