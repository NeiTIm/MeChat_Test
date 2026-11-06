import { readFile, writeFile } from 'node:fs/promises'
const path = 'src/features/auth/AuthPage.jsx'
await writeFile(path, (await readFile(path, 'utf8')).replace("aria-label={t('auth:backToLogin')}", "aria-label={t('auth:brandBackToLogin')}"))
const test = 'tests/e2e/auth-client.spec.js'
await writeFile(test, (await readFile(test, 'utf8')).replace("await expect(page.getByText('Địa chỉ email không hợp lệ.')).toBeVisible()", "await expect(page.getByText('Vui lòng nhập thông tin này.')).toBeVisible()")
  .replaceAll("name: 'Lưu',", "name: 'Lưu thay đổi',").replace(".disableRules(['color-contrast'])", ''))
const locale = 'src/i18n/locales/vi/auth.json'
const data = JSON.parse(await readFile(locale, 'utf8'))
data.brandBackToLogin = 'MeChat — quay lại đăng nhập'
await writeFile(locale, JSON.stringify(data, null, 2) + '\n')
