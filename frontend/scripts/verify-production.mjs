import { readdir, readFile } from 'node:fs/promises'
import { join } from 'node:path'

const root = new URL('../dist/', import.meta.url)
const forbidden = ['design-self', 'design-message-', 'The Creative Corner', 'DESIGN PREVIEW', '/design/private', 'Explore the design preview']
async function inspect(directory) {
  for (const entry of await readdir(directory, { withFileTypes: true })) {
    const path = join(directory, entry.name)
    if (entry.isDirectory()) await inspect(path)
    else if (/\.(js|html)$/.test(entry.name)) {
      const source = await readFile(path, 'utf8')
      for (const marker of forbidden) if (source.includes(marker)) throw new Error(`Design fixture marker found in production: ${entry.name}`)
      if (/DesignReview/.test(entry.name)) throw new Error('Design review chunk found in production')
    }
  }
}
await inspect(decodeURIComponent(root.pathname).replace(/^\/(\w:)/, '$1'))
console.log('PASS: production JS/HTML excludes design routes, fixtures and preview labels.')
