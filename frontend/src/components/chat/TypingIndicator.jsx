import { useTranslation } from 'react-i18next'

// Keep a small, stable slot outside the scrolling history and above the composer.
export function TypingIndicator({ names = [] }) {
  const { t } = useTranslation()
  const text =
    names.length > 2
      ? t('messages:typingOthers', { names: names.slice(0, 2).join(', '), count: names.length - 2 })
      : names.length
        ? t('messages:typingNamed', { names: names.join(', ') })
        : ''
  return (
    <div
      className="typing-indicator"
      role="status"
      aria-live="polite"
      aria-atomic="true"
    >
      {text && (
        <>
          <span
            className="typing-label"
            title={text}
          >
            {text}
          </span>
          <span
            className="typing-dots"
            aria-hidden="true"
          >
            <i />
            <i />
            <i />
          </span>
        </>
      )}
    </div>
  )
}
