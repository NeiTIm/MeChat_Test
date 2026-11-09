import { useState } from 'react'
import { useInfiniteQuery, useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Pin } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { StatePanel } from '@/components/StatePanel'
import { ConfirmAction } from '@/components/ConfirmAction'
import { messagesApi } from '@/features/messages/api'
import { queryClient } from '@/lib/query-client'
import { formatDate, formatTime } from '@/i18n/format'
import { groupsApi } from './api'

export function GroupPins({ groupId, canManage }) {
  const { t } = useTranslation()
  const [open, setOpen] = useState(false)
  const summary = useQuery({
    queryKey: ['workspace', 'groupPinsSummary', groupId],
    queryFn: ({ signal }) => groupsApi.pins(groupId, signal),
  })
  const pins = useInfiniteQuery({
    queryKey: ['workspace', 'groupPins', groupId],
    queryFn: ({ pageParam, signal }) => groupsApi.pinPage(groupId, pageParam, signal),
    initialPageParam: undefined,
    getNextPageParam: (page) => (page.hasMore ? page.nextCursor : undefined),
    enabled: open,
  })
  const items = [
    ...new Map(
      (pins.data?.pages || []).flatMap((page) => page.items).map((item) => [item.messageId, item]),
    ).values(),
  ]
  return (
    <Dialog
      open={open}
      onOpenChange={setOpen}
    >
      <DialogTrigger asChild>
        <Button variant="outline">
          <Pin size={17} />
          {t('groups:pinsTitle')}
          {summary.data && <span className="tab-count">{summary.data.length}</span>}
        </Button>
      </DialogTrigger>
      <DialogContent className="details-dialog">
        <DialogHeader>
          <DialogTitle>{t('groups:pinsTitle')}</DialogTitle>
          <DialogDescription>{t('groups:pinsDescription')}</DialogDescription>
        </DialogHeader>
        {pins.isPending ? (
          <StatePanel loading />
        ) : pins.error ? (
          <StatePanel
            error={pins.error}
            onRetry={pins.refetch}
          />
        ) : !items.length ? (
          <StatePanel title={t('groups:noPins')} />
        ) : (
          <div className="message-search-results">
            {items.map((pin) => (
              <PinnedMessage
                key={pin.messageId}
                pin={pin}
                groupId={groupId}
                canManage={canManage}
              />
            ))}
            {pins.hasNextPage && (
              <Button
                variant="outline"
                disabled={pins.isFetchingNextPage}
                onClick={() => pins.fetchNextPage()}
              >
                {pins.isFetchingNextPage ? t('common:saving') : t('common:loadMore')}
              </Button>
            )}
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
function PinnedMessage({ pin, groupId, canManage }) {
  const { t } = useTranslation()
  const message = useQuery({
    queryKey: ['workspace', 'message', pin.messageId],
    queryFn: ({ signal }) => messagesApi.get(pin.messageId, signal),
  })
  return (
    <article>
      <time>
        {t('groups:pinnedAt', {
          date: `${formatDate(pin.pinnedAt)} · ${formatTime(pin.pinnedAt)}`,
        })}
      </time>
      {message.isPending ? (
        <StatePanel
          compact
          loading
        />
      ) : message.error ? (
        <StatePanel
          compact
          error={message.error}
          onRetry={message.refetch}
        />
      ) : (
        <p>
          {message.data.recalled
            ? t('groups:pinUnavailable')
            : message.data.content || t('messages:replyAttachment')}
        </p>
      )}
      {canManage && (
        <ConfirmAction
          label={t('groups:unpin')}
          description={t('groups:unpinConfirmation')}
          onConfirm={async () => {
            await groupsApi.unpin(groupId, pin.messageId)
            await queryClient.invalidateQueries({ queryKey: ['workspace'] })
          }}
        />
      )}
    </article>
  )
}
