import axios from 'axios'
import { http } from '@/lib/http'
export const reportsApi = {
  create: async (input) => (await http.post('/reports', input)).data,
  mine: async ({ signal }) => (await http.get('/reports/me', { signal })).data,
}
// Separate transport: appeal scope must never refresh or overwrite the normal chat identity.
const appealHttp = axios.create({ baseURL: '/api/appeals', timeout: 20000 })
export const appealsApi = {
  authenticate: async (input) => (await appealHttp.post('/authenticate', input)).data,
  mine: async (token, signal) => (await appealHttp.get('/me', { signal, headers: { Authorization: `Bearer ${token}` } })).data,
  create: async (token, reason) => (await appealHttp.post('', { reason }, { headers: { Authorization: `Bearer ${token}` } })).data,
}
