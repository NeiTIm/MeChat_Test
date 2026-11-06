import { readFile, writeFile } from 'node:fs/promises'
const path = 'src/components/chat/ChatWorkspace.jsx'
let text = await readFile(path, 'utf8')
text = text.replace('  titleOverride\n', '  titleOverride,\n  renderAttachment\n')
text = text.replace('message.attachments?.map(attachment => <span', 'message.attachments?.map(attachment => renderAttachment ? renderAttachment(attachment) : <span')
await writeFile(path, text)
const workspace = 'src/features/conversations/WorkspacePage.jsx'
text = await readFile(workspace, 'utf8')
text = text.replace("import { GroupPins }", "import { AttachmentPreview } from '@/features/files/AttachmentPreview';\nimport { GroupPins }")
text = text.replace('  renderMessageContext={(message)', '  renderAttachment={(file) => <AttachmentPreview key={file.id} file={file} />}\n  renderMessageContext={(message)')
await writeFile(workspace, text)
