import { readFile, writeFile } from 'node:fs/promises'
const path = 'src/components/chat/Composer.jsx'
let source = await readFile(path, 'utf8')
source = source.replace('t("chat:edit-your-sample-message")', '(preview ? t("chat:edit-your-sample-message") : t("messages:editPlaceholder"))')
  .replace('t("chat:save-sample-edit")', '(preview ? t("chat:save-sample-edit") : t("messages:saveEdit"))')
  .replace('onChange={event => setDraft(event.target.value)}', 'onChange={event => { setDraft(event.target.value); onTyping?.(!!event.target.value.trim()); }} onBlur={() => onTyping?.(false)}')
await writeFile(path, source)
