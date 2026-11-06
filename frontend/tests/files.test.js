import { describe, expect, it } from 'vitest'
import { validateUpload } from '@/features/files/validation'
import '@/i18n'
describe('upload contract validation', () => {
  it('checks empty, unsupported, avatar and size limits without trusting MIME', () => {
    const file = (name, size) => ({ name, size, type: 'image/jpeg' })
    expect(validateUpload(file('anh.PNG', 5 * 1024 * 1024), 'Avatar')).toBeNull()
    expect(validateUpload(file('anh.png', 5 * 1024 * 1024 + 1), 'Avatar')).toContain('5 MB')
    expect(validateUpload(file('anh.webp', 10 * 1024 * 1024 + 1))).toContain('10 MB')
    expect(validateUpload(file('tai-lieu.pdf', 25 * 1024 * 1024))).toBeNull()
    expect(validateUpload(file('tai-lieu.pdf', 25 * 1024 * 1024 + 1))).toContain('25 MB')
    expect(validateUpload(file('tai-lieu.pdf', 20), 'Avatar')).toContain('Ảnh đại diện')
    expect(validateUpload(file('nguy-hiem.html', 20))).toContain('Hỗ trợ')
    expect(validateUpload(file('rong.txt', 0))).toContain('không có nội dung')
    expect(validateUpload(file('loi\n.txt', 10))).toContain('Tên tệp')
  })
})
