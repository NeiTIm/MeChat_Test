import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { formatNumber } from '@/i18n/format'
import { statusLabel } from '@/i18n'
import { messagesApi } from '@/features/messages/api'
import { useMessageMetadata } from '@/features/messages/message-metadata'

export function MessageReply({ message }) {
  const { t } = useTranslation()
  const {
    ref,
    query: context,
    userId,
    active,
  } = useMessageMetadata('context', message, !!message.replyToMessageId && !message.recalled)
  const source = useQuery({
    queryKey: ['workspace', 'message', message.replyToMessageId, userId, message.conversationId],
    queryFn: ({ signal }) => messagesApi.get(message.replyToMessageId, signal),
    staleTime: Infinity,
    refetchOnWindowFocus: false,
    retry: false,
    enabled: !!(
      active &&
      userId &&
      message.replyToMessageId &&
      context.data?.reply?.available &&
      context.data?.reply?.content === 'Attachment'
    ),
  })
  if (!message.replyToMessageId || message.recalled) return null
  const reply = context.data?.reply
  // Resolve the ambiguous server sentinel before translating; user-entered "Attachment" stays intact.
  const content = reply?.available
    ? reply.content === 'Attachment' && source.data?.content === null
      ? t('messages:replyAttachment')
      : reply.content
    : t('messages:replyUnavailable')
  return (
    <div ref={ref}>{reply && <blockquote className="reply-preview">{content}</blockquote>}</div>
  )
}
export function MessageReactions({ message }) {
  const { t } = useTranslation()
  const { ref, query: context } = useMessageMetadata('context', message, !message.recalled)
  if (message.recalled) return null
  const limited = context.error?.response?.status === 429
  return (
    <div
      ref={ref}
      className="message-reactions"
    >
      {context.error ? (
        <button
          className="metadata-retry"
          onClick={() => context.refetch()}
          title={t(limited ? 'messages:metadataRateLimited' : 'messages:loadContext')}
          aria-label={limited ? t('messages:metadataRateLimited') : undefined}
        >
          {t(limited ? 'messages:metadataPaused' : 'messages:contextUnavailable')}
        </button>
      ) : (
        context.data?.reactions.map((reaction) => (
          <span
            key={reaction.emoji}
            className={context.data.myReaction === reaction.emoji ? 'mine' : ''}
            aria-label={t('messages:reactionLabel', {
              emoji: reaction.emoji,
              count: formatNumber(reaction.count),
            })}
          >
            {reaction.emoji} {formatNumber(reaction.count)}
          </span>
        ))
      )}
    </div>
  )
}
export function MessageReceipt({ message, own }) {
  const { t } = useTranslation()
  const { ref, query: receipt } = useMessageMetadata('receipt', message, own)
  if (!own) return null
  const limited = receipt.error?.response?.status === 429
  return (
    <span ref={ref}>
      {receipt.data && (
        <span
          title={t('messages:receiptCounts', {
            delivered: formatNumber(receipt.data.deliveredCount),
            seen: formatNumber(receipt.data.seenCount),
          })}
        >
          {statusLabel(receipt.data.status)}
        </span>
      )}
      {receipt.error && (
        <button
          className="metadata-retry"
          onClick={() => receipt.refetch()}
          title={limited ? t('messages:metadataRateLimited') : undefined}
          aria-label={limited ? t('messages:metadataRateLimited') : undefined}
        >
          {t(limited ? 'messages:metadataPaused' : 'messages:receiptUnavailable')}
        </button>
      )}
    </span>
  )
}
