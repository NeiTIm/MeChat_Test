import { http } from '@/lib/http'
export const adminApi = {
  dashboard: async ({ signal }) => (await http.get('/admin/dashboard', { signal })).data,
  list: async (section, skip, signal) => (await http.get(`/admin/${section}`, { params: { skip }, signal })).data,
  claim: async (id) => (await http.post(`/admin/reports/${id}/claim`)).data,
  case: async (id, signal) => (await http.get(`/admin/reports/${id}`, { signal })).data,
  evidence: async (id, evidenceId) => {
    const result = await http.get(`/admin/reports/${id}/evidence/${evidenceId}/file`, { responseType: 'blob' })
    const disposition = result.headers['content-disposition'] || ''
    let fileName = 'bang-chung'
    try { fileName = decodeURIComponent(disposition.match(/filename\*=UTF-8''([^;]+)/i)?.[1] || disposition.match(/filename="?([^";]+)"?/i)?.[1] || fileName).split(/[\\/]/).at(-1) } catch { /* Use a neutral download name when header encoding is malformed. */ }
    return { blob: result.data, fileName }
  },
  resolve: async (id, input) => (await http.post(`/admin/reports/${id}/resolve`, input)).data,
  warn: async (id, input) => (await http.post(`/admin/users/${id}/warn`, input)).data,
  suspend: async (id, input) => (await http.post(`/admin/users/${id}/suspend`, input)).data,
  unsuspend: async (id, input) => (await http.post(`/admin/users/${id}/unsuspend`, input)).data,
  disableGroup: async (id, input) => (await http.post(`/admin/groups/${id}/disable`, input)).data,
  reviewAppeal: async (id, input) => (await http.post(`/admin/appeals/${id}/review`, input)).data,
  retention: async ({ signal }) => (await http.get('/admin/retention/preview', { signal })).data,
  fileRetention: async ({ signal }) => (await http.get('/admin/retention/files/preview', { signal })).data,
  plan: async (kind, offset, signal) => (await http.get('/admin/retention/plan', { params: { kind, offset, limit: 30 }, signal })).data,
  hold: async (kind, id, input) => (await http.put(`/admin/retention/${kind}/${id}/hold`, input)).data,
}
