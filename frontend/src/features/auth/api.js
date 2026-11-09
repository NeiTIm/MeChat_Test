import { http, csrfToken, clearSession, adoptSession } from '@/lib/http'

export const authApi = {
  register: async (input) => (await http.post('/auth/register', input, { skipAuth: true })).data,
  verifyEmail: async (input) =>
    (await http.post('/auth/verify-email', input, { skipAuth: true })).data,
  resendVerification: async (input) =>
    (await http.post('/auth/resend-verification', input, { skipAuth: true })).data,
  forgotPassword: async (input) =>
    (await http.post('/auth/forgot-password', input, { skipAuth: true })).data,
  resetPassword: async (input) =>
    (await http.post('/auth/reset-password', input, { skipAuth: true })).data,
  reactivate: async (input) =>
    (await http.post('/auth/reactivate', input, { skipAuth: true })).data,
  changePassword: async (input) => (await http.post('/auth/change-password', input)).data,
  deactivate: async (input) => {
    await http.post('/auth/deactivate', input)
    clearSession()
  },
  sessions: async ({ signal }) => (await http.get('/auth/sessions', { signal })).data,
  revokeSession: async (id) => (await http.delete(`/auth/sessions/${id}`)).data,
  logoutOthers: async () => (await http.post('/auth/logout-others')).data,
  async login(values) {
    const { data } = await http.post(
      '/auth/login',
      { ...values, deviceName: 'MeChat Web' },
      { skipAuth: true },
    )
    adoptSession(data)
    return data
  },
  async logout() {
    await http.post('/auth/logout', null, { headers: { 'X-CSRF-Token': csrfToken() } })
    clearSession()
  },
}
