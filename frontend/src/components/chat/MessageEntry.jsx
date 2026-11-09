import { memo } from 'react'
import { useTranslation } from 'react-i18next'
import { Heart } from 'lucide-react'
import { Avatar } from '@/components/Avatar'
import { formatTime } from '@/i18n/format'

// Preserve unchanged history rows while a new page, connection hint or query
// updates. Props are server message objects and stable feature renderers.
export const MessageEntry = memo(function MessageEntry({
  message,
  own,
  author,
  group,
  preview,
  date,
  highlighted,
  focused,
  renderMessageActions,
  renderMessageContext,
  renderMessageReactions,
  renderMessageMeta,
  renderAttachment,
}) {
  const { t } = useTranslation()
  return (
    <div
      className={`message-entry ${highlighted ? 'message-highlighted' : ''}`}
      data-message-id={message.id}
      tabIndex={focused ? -1 : undefined}
    >
      {date && (
        <div className="date-divider">
          <span>{date}</span>
        </div>
      )}
      <article
        className={`message-row ${own ? 'own' : ''}`}
        aria-label={t('chat:message-from-value0', { value0: own ? t('common:you') : author })}
      >
        {!own && (
          <Avatar
            name={author}
            small
            tone={group ? 2 : 1}
          />
        )}
        <div className="message-content">
          {group && !own && <span className="message-author">{author}</span>}
          <div className={`message-bubble ${message.recalled ? 'recalled' : ''}`}>
            {message.recalled ? (
              t('chat:this-message-was-recalled')
            ) : (
              <>
                {message.isForwarded && (
                  <span className="forwarded-label">{t('chat:forwarded')}</span>
                )}
                {message.previewReply && (
                  <blockquote className="reply-preview">{message.previewReply}</blockquote>
                )}
                {renderMessageContext?.(message)}
                <p>{message.content}</p>
                {message.attachments?.map((attachment) =>
                  renderAttachment ? (
                    renderAttachment(attachment)
                  ) : (
                    <span
                      className="attachment-label"
                      key={attachment.id}
                    >
                      {t('chat:attachment')}
                      {attachment.fileName}
                    </span>
                  ),
                )}
              </>
            )}
          </div>
          {renderMessageReactions?.(message)}
          {message.previewReaction && !message.recalled && (
            <span className="reaction-preview">
              <Heart
                size={12}
                fill="currentColor"
              />{' '}
              1 <span className="sr-only">{t('chat:sample-heart-reaction')}</span>
            </span>
          )}
          <div className="message-time">
            <time dateTime={message.createdAt}>{formatTime(message.createdAt)}</time>
            {message.editedAt && <span>{t('chat:edited')}</span>}
            {renderMessageMeta
              ? renderMessageMeta(message, own)
              : preview && own && <span>{t('chat:sample-message')}</span>}
          </div>
          {renderMessageActions?.(message, own)}
        </div>
      </article>
    </div>
  )
})
