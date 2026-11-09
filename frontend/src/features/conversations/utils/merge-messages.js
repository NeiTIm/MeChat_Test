export function mergeMessages(pages = []) {
  const messagesById = new Map()
  for (const page of pages) {
    for (const message of page.items) {
      if (!messagesById.has(message.id)) messagesById.set(message.id, message)
    }
  }
  return [...messagesById.values()].sort((first, second) => first.sequence - second.sequence)
}
