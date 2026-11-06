import { readFile, writeFile } from 'node:fs/promises'
const path = 'tests/e2e/auth.live.spec.js'
let source = await readFile(path, 'utf8')
source = "import { setAuthInputs } from './secure-auth-input';\n" + source
const from = source.indexOf('    await page.getByLabel(')
const to = source.indexOf("    await page.getByRole('button'", from)
if (from < 0 || to < 0) throw new Error('Live input anchor missing')
source = source.slice(0, from) + "    await page.getByRole('button', { name: 'Đăng nhập', exact: true }).waitFor();\n    await setAuthInputs(page, process.env.MECHAT_E2E_IDENTIFIER, process.env.MECHAT_E2E_PASSWORD);\n" + source.slice(to)
await writeFile(path, source)
