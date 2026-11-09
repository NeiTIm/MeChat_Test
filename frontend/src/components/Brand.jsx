import { MessageSquare } from 'lucide-react'

export function Brand({ compact = false }) {
  return (
    <span
      className={`brand ${compact ? 'brand-compact' : ''}`}
      aria-label="MeChat"
    >
      <span
        className="brand-mark"
        aria-hidden="true"
      >
        <MessageSquare
          size={25}
          strokeWidth={2}
        />
        <span className="brand-dots">···</span>
      </span>
      {!compact && (
        <span>
          mechat<span className="brand-period">.</span>
        </span>
      )}
    </span>
  )
}
