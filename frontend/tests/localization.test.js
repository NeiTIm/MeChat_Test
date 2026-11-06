import { describe, expect, it } from 'vitest';
import i18n, { roleLabel, statusLabel } from '@/i18n';
import { formatDate, formatDay, formatTime, formatRelative, formatNumber, DISPLAY_TIME_ZONE } from '@/i18n/format';
import { authSchemas } from '@/features/auth/schemas';
import { errorMessage } from '@/lib/errors';
describe('Vietnamese-first localization contract', () => {
  it('uses vi-VN with Vietnamese fallback and feature namespaces', () => {
    expect(i18n.language).toBe('vi-VN');
    expect(i18n.options.fallbackLng).toContain('vi');
    expect(i18n.t('auth:pages.register.title')).toBe('Tạo tài khoản MeChat');
    expect(roleLabel('Owner')).toBe('Chủ nhóm');
    expect(statusLabel('Active')).toBe('Đang hoạt động');
    expect(roleLabel('unrecognized')).toBe('Chưa xác định vai trò');
  });
  it('formats timestamps without changing API data, including Vietnam midnight', () => {
    const value = '2026-11-05T17:30:00Z';
    expect(DISPLAY_TIME_ZONE).toBe('Asia/Ho_Chi_Minh');
    expect(formatDate(value)).toBe('06/11/2026');
    expect(formatTime(value)).toBe('00:30');
    expect(formatDay(value, new Date('2026-11-06T05:00:00Z'))).toBe('Hôm nay');
    expect(formatDay(value, new Date('2026-11-07T05:00:00Z'))).toBe('Hôm qua');
    expect(formatRelative('2026-11-06T06:55:00Z', new Date('2026-11-06T07:00:00Z'))).toBe('5 phút trước');
    expect(formatNumber(12345)).toBe('12.345');
    expect(value).toBe('2026-11-05T17:30:00Z');
  });
  it('validates passwords and confirmation in Vietnamese without trimming passwords', () => {
    const schemas = authSchemas(i18n.t.bind(i18n));
    const invalid = schemas.register.safeParse({
      email: 'valid@example.invalid',
      username: 'abc',
      password: 'short',
      confirmPassword: 'different'
    });
    expect(invalid.success).toBe(false);
    expect(invalid.error.issues.map(issue => issue.message)).toContain('Mật khẩu cần ít nhất 12 ký tự.');
    expect(invalid.error.issues.map(issue => issue.message)).toContain('Mật khẩu xác nhận chưa trùng khớp.');
    const password = '  long-password  ';
    expect(schemas.reactivate.parse({
      identifier: ' name ',
      password
    }).password).toBe(password);
  });
  it('does not expose arbitrary backend messages or validation exceptions', () => {
    const sensitive = 'SQL exception: private connection string';
    expect(errorMessage({
      response: {
        status: 500,
        data: {
          message: sensitive,
          errors: {
            Password: [sensitive]
          }
        }
      }
    })).toBe('Hệ thống gặp sự cố. Vui lòng thử lại sau.');
    expect(errorMessage({
      response: {
        status: 409,
        data: {
          code: 'USERNAME_UNAVAILABLE_OR_COOLDOWN'
        }
      }
    })).toContain('Tên đăng nhập');
  });
});
