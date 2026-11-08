import { sanitizeMessageCache } from './cache'

const history = q => q.queryKey.at(-1) === 'messages'
const overview = q => ['unreadSummary', 'threadUnread', 'requestCount', 'messageRequests'].includes(q.queryKey[1]) || q.queryKey[2] === 'list'
const invalidate = (client, predicate) => client.invalidateQueries({ queryKey: ['workspace'], predicate }, { cancelRefetch: false })

export function refreshMessageThread(client, conversationId) {
  return invalidate(client, q => overview(q) || (history(q) || q.queryKey[1] === 'messageSearch') && (!conversationId || q.queryKey[2] === conversationId))
}

export function refreshMessageContext(client, messageId, userId, edited = false) {
  if (edited && messageId) client.invalidateQueries({ queryKey: ['workspace', 'editHistory', messageId] }, { cancelRefetch: false })
  return invalidate(client, q => q.meta?.messageMetadata && q.meta.kind === 'context' && (!userId || q.meta.userId === userId) &&
    (!messageId || q.meta.messageId === messageId || q.meta.replyToMessageId === messageId))
}

// Receipt/reaction hints must never reload history or every workspace query.
export function handleMessageHint(client, name, payload = {}, userId) {
  const { messageId, conversationId } = payload
  if (name === 'MessageDelivered' || name === 'MessageSeen') {
    const seenBySelf = name === 'MessageSeen' && payload.userId === userId
    return invalidate(client, q => {
      if (seenBySelf) return ['unreadSummary', 'threadUnread', 'requestCount', 'messageRequests'].includes(q.queryKey[1])
      const meta = q.meta
      return meta?.messageMetadata && meta.kind === 'receipt' && meta.userId === userId &&
        (!conversationId || meta.conversationId === conversationId) && (!messageId || meta.messageId === messageId) &&
        (!Number.isSafeInteger(payload.lastReadSequence) || meta.sequence <= payload.lastReadSequence)
    })
  }
  if (name === 'MessageReactionChanged') return refreshMessageContext(client, messageId, userId)
  if (name === 'MessageEdited') {
    client.invalidateQueries({ queryKey: ['workspace', 'message', messageId] }, { cancelRefetch: false })
    refreshMessageContext(client, messageId, userId, true)
  }
  if (name === 'MessageRecalled' || name === 'MessageDeletedForMe') {
    if (!messageId) return client.resetQueries({ queryKey: ['workspace'] }) // fail closed for unknown privacy hints
    sanitizeMessageCache(messageId, name === 'MessageRecalled', client)
  }
  return refreshMessageThread(client, conversationId)
}
