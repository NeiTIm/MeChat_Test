import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { StatePanel } from '@/components/StatePanel'
import { errorMessage } from '@/lib/errors'
import { formatDate, formatTime } from '@/i18n/format'
import { ForwardDestinations } from '@/features/messages/ForwardDestinations'
import { ReportForm } from '@/features/moderation/components/ReportForm'

export function MessageActionDialog({
  t,
  operation,
  setOperation,
  action,
  message,
  versions,
  destination,
  setDestination,
}) {
  return (
    <Dialog
      open={!!operation}
      onOpenChange={(open) => {
        if (!open && !action.isPending) {
          setOperation(null)
          action.reset()
        }
      }}
    >
      <DialogContent className="details-dialog">
        <DialogHeader>
          <DialogTitle>
            {t(
              operation === 'report'
                ? 'reports:report'
                : operation === 'pin'
                  ? 'groups:pin'
                  : `messages:${operation || 'options'}`,
            )}
          </DialogTitle>
          <DialogDescription>
            {t(
              operation === 'report'
                ? 'reports:reportDescription'
                : operation === 'pin'
                  ? 'groups:pinsDescription'
                  : operation === 'recall'
                    ? 'messages:recallDescription'
                    : operation === 'delete'
                      ? 'messages:deleteDescription'
                      : operation === 'forward'
                        ? 'messages:forwardDescription'
                        : operation === 'react'
                          ? 'messages:reactionDescription'
                          : 'messages:history',
            )}
          </DialogDescription>
        </DialogHeader>
        {action.error && (
          <p
            role="alert"
            className="form-feedback feedback-error"
          >
            {errorMessage(action.error)}
          </p>
        )}
        {operation === 'report' ? (
          <ReportForm
            targetType="Message"
            targetId={message.id}
          />
        ) : operation === 'history' ? (
          versions.isPending ? (
            <StatePanel loading />
          ) : versions.error ? (
            <StatePanel
              error={versions.error}
              onRetry={versions.refetch}
            />
          ) : versions.data.versions.length === 0 ? (
            <StatePanel title={t('messages:noHistory')} />
          ) : (
            <ol className="message-versions">
              {versions.data.versions.map((version) => (
                <li key={version.version}>
                  <strong>
                    {t('messages:version', {
                      version: version.version,
                      date: `${formatDate(version.editedAt)} · ${formatTime(version.editedAt)}`,
                    })}
                  </strong>
                  <p>{version.content}</p>
                </li>
              ))}
            </ol>
          )
        ) : operation === 'react' ? (
          <div className="reaction-picker">
            {['👍', '❤️', '😂', '😮', '😢', '🎉'].map((emoji) => (
              <Button
                key={emoji}
                variant="outline"
                disabled={action.isPending}
                onClick={() => action.mutate(emoji)}
                aria-label={t('messages:reactionLabel', { emoji, count: 1 })}
              >
                {emoji}
              </Button>
            ))}
            <Button
              variant="ghost"
              disabled={action.isPending}
              onClick={() => action.mutate(null)}
            >
              {t('messages:removeReaction')}
            </Button>
          </div>
        ) : (
          <>
            {operation === 'forward' && (
              <ForwardDestinations
                id={`forward-${message.id}`}
                value={destination}
                onChange={setDestination}
                disabled={action.isPending}
              />
            )}

            <div className="confirmation-actions">
              <Button
                variant="outline"
                disabled={action.isPending}
                onClick={() => setOperation(null)}
              >
                {t('common:cancel')}
              </Button>
              <Button
                variant={
                  operation === 'recall' || operation === 'delete' ? 'destructive' : 'default'
                }
                disabled={action.isPending || (operation === 'forward' && !destination)}
                onClick={() => action.mutate()}
              >
                {action.isPending ? t('common:saving') : t('common:confirm')}
              </Button>
            </div>
          </>
        )}
      </DialogContent>
    </Dialog>
  )
}
