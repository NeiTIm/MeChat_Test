import { readFile, writeFile } from 'node:fs/promises'
const path = 'src/features/groups/GroupPages.jsx'
let source = await readFile(path, 'utf8')
source = source.replace("import { useState }", "import { useEffect, useState }")
  .replace('id="invite-user" fields={[]}', 'id="invite-user" submitDisabled={!selected.length} fields={[]}')
  .replace("    if (window.location.hash) window.history.replaceState(window.history.state, '', `${window.location.pathname}${window.location.search}`)\n", '')
  .replace('  const [resolved, setResolved] = useState(null)', "  useEffect(() => { if (window.location.hash) window.history.replaceState(window.history.state, '', `${window.location.pathname}${window.location.search}`) }, [])\n  const [resolved, setResolved] = useState(null)")
await writeFile(path, source)
