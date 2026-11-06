import { readFile, writeFile } from 'node:fs/promises'
const file = 'src/components/chat/ChatWorkspace.jsx'
await writeFile(file, (await readFile(file, 'utf8')).replace('<h2 title={selected.name}>{selected.name}</h2>', '<h1 title={selected.name}>{selected.name}</h1>'))
const css = 'src/styles/index.css'
await writeFile(css, (await readFile(css, 'utf8')).replaceAll('.thread-heading h2', '.thread-heading :is(h1, h2)'))
