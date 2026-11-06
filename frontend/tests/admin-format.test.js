import { expect, it } from 'vitest'
import { auditDetails } from '@/features/admin/format'
it('localizes machine audit metadata and preserves authored content', () => {
  expect(auditDetails({ action: 'RetentionDryRun', details: 'offset=1000;limit=30;no deletion' })).toBe('Vị trí bắt đầu: 1.000 · Tối đa 30 mục · Không thực hiện xóa.')
  expect(auditDetails({ action: 'ReviewAppeal', details: 'Accepted: Keep this authored note unchanged.' })).toBe('Đã chấp nhận · Keep this authored note unchanged.')
  expect(auditDetails({ action: 'WarnUser', details: 'Accepted: This is user-authored text.' })).toBe('Accepted: This is user-authored text.')
})
