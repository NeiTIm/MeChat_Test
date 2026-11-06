import { readFile, writeFile } from 'node:fs/promises'
import namespaces from './stage01-copy.mjs'
for (const [ns, values] of Object.entries(namespaces)) {
  const path = `src/i18n/locales/vi/${ns}.json`
  let existing = {}
  try { existing = JSON.parse(await readFile(path, 'utf8')) } catch (error) { if (error.code !== 'ENOENT') throw error }
  await writeFile(path, JSON.stringify({ ...existing, ...values }, null, 2) + '\n')
}
