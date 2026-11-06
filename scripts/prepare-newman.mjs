import { execFileSync } from 'node:child_process'
import { randomUUID } from 'node:crypto'
import { readFile, readdir, writeFile, mkdir } from 'node:fs/promises'
import { resolve } from 'node:path'

const root = resolve(import.meta.dirname, '..')
const baseUrl = process.env.MECHAT_SMOKE_URL || process.env.WEBCHAT_SMOKE_URL || 'http://localhost:5080'
const url = new URL(baseUrl)
if (!['localhost', '127.0.0.1'].includes(url.hostname) || url.protocol !== 'http:' || url.pathname !== '/')
  throw new Error('Only the explicitly local development API is permitted.')
const preflight = execFileSync('sqlcmd', ['-S', String.raw`lpc:.\MSSQLSERVER01`, '-d', 'MeChat', '-E', '-C', '-b', '-h', '-1', '-W', '-Q',
  "SET NOCOUNT ON; SELECT DB_NAME(); SELECT CONVERT(nvarchar(60), DATABASEPROPERTYEX(DB_NAME(),'Status'));"], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] }).trim().split(/\r?\n/).map(line => line.trim()).filter(Boolean)
if (preflight[0] !== 'MeChat' || preflight[1] !== 'ONLINE') throw new Error('Target must be verified MeChat ONLINE. No changes attempted.')
const values = [{ key: 'baseUrl', value: baseUrl, enabled: true }]
const directory = resolve(root, 'backend/MeChat.Api/private-uploads/dev-mail')
async function post(path, body, status) {
  const response = await fetch(`${baseUrl}/api${path}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
  if (response.status !== status) throw new Error(`${path}: expected ${status}, received ${response.status}`)
  return response.headers.get('content-type')?.includes('application/json') ? response.json() : null
}
for (const actor of ['a', 'b', 'c']) {
  const suffix = randomUUID().replaceAll('-', '').slice(0, 12)
  const email = `acceptance-${suffix}@example.invalid`
  const password = `Synthetic-${randomUUID()}!Aa1`
  await post('/auth/register', { email, username: `ac${suffix}`, password }, 201)
  let token
  for (const name of await readdir(directory)) {
    if (!name.endsWith('.json')) continue
    const mail = JSON.parse(await readFile(resolve(directory, name), 'utf8'))
    if (mail.email === email && mail.purpose === 'verify-email') { token = mail.token; break }
  }
  if (!token) throw new Error('Development verification mail not found; no bypass allowed.')
  await post('/auth/verify-email', { token }, 204)
  values.push({ key: `${actor}Email`, value: email, enabled: true, type: 'secret' },
    { key: `${actor}Password`, value: password, enabled: true, type: 'secret' })
}
const output = resolve(root, `backend/MeChat.Api/private-uploads/newman-acceptance-${randomUUID()}.json`)
await mkdir(resolve(root, 'backend/MeChat.Api/private-uploads'), { recursive: true })
await writeFile(output, JSON.stringify({ name: 'Synthetic local acceptance', values }, null, 2))
console.log(`Prepared fresh synthetic verified accounts. Environment saved only under ignored private-uploads: ${output}`)
