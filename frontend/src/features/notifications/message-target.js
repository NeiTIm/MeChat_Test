export function notificationMessageTarget(notification, message) {
  const base = { Message: '/chat', GroupMessage: '/groups', MessageRequest: '/message-requests' }[notification.type]
  if (!base || message?.id !== notification.referenceId || !message.conversationId ||
    !Number.isSafeInteger(message.sequence) || message.sequence <= 0 || message.sequence >= Number.MAX_SAFE_INTEGER) {
    throw { response: { status: 404 } }
  }
  return `${base}/${encodeURIComponent(message.conversationId)}?${new URLSearchParams({ at: String(message.sequence), message: message.id })}`
}
