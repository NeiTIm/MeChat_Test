import { MessageActionDialog } from './components/MessageActionDialog'
import { useRef, useState } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { DropdownMenu } from 'radix-ui'
import { MoreHorizontal, Reply, Pencil, Undo2, Trash2, Forward, Heart, History } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { queryClient } from '@/lib/query-client'
import { messagesApi } from './api'
import { sanitizeMessageCache } from './cache'
import { showSuccess } from '@/stores/ui-feedback-store'
import { refreshMessageContext, refreshMessageThread } from './message-events'

export { MessageReply } from '@/features/messages/components/MessageMetadata'
export { MessageReactions } from '@/features/messages/components/MessageMetadata'
export { MessageReceipt } from '@/features/messages/components/MessageMetadata'
export function MessageActions({ message, own, onCompose, canSend = true, extraItems, onPin }) {
  const { t } = useTranslation()
  const [operation, setOperation] = useState(null)
  const [destination, setDestination] = useState('')
  const retryForward = useRef(null)
  const action = useMutation({
    mutationFn: async (emoji) => {
      if (operation === 'recall') await messagesApi.recall(message.id)
      else if (operation === 'delete') await messagesApi.deleteForMe(message.id)
      else if (operation === 'pin') await onPin(message.id)
      else if (operation === 'forward') {
        if (!retryForward.current || retryForward.current.destination !== destination)
          retryForward.current = { destination, clientMessageId: crypto.randomUUID() }
        await messagesApi.forward(message.id, destination, retryForward.current.clientMessageId)
      } else if (emoji) await messagesApi.react(message.id, emoji)
      else await messagesApi.removeReaction(message.id)
      if (operation === 'recall' || operation === 'delete')
        sanitizeMessageCache(message.id, operation === 'recall')
      showSuccess(t('messages:actionSaved'))
      if (operation === 'react') await refreshMessageContext(queryClient, message.id)
      else if (operation === 'pin')
        await queryClient.invalidateQueries({
          queryKey: ['workspace'],
          predicate: (q) =>
            ['groupPins', 'groupPinsSummary'].includes(q.queryKey[1]) &&
            q.queryKey[2] === message.conversationId,
        })
      else
        await refreshMessageThread(
          queryClient,
          operation === 'forward' ? destination : message.conversationId,
        )
      setOperation(null)
    },
  })
  const versions = useQuery({
    queryKey: ['workspace', 'editHistory', message.id],
    queryFn: ({ signal }) => messagesApi.history(message.id, signal),
    enabled: operation === 'history',
  })
  const canEdit =
    own &&
    canSend &&
    message.content &&
    Date.now() >= new Date(message.createdAt).getTime() &&
    Date.now() - new Date(message.createdAt).getTime() <= 15 * 60_000
  return (
    <>
      <DropdownMenu.Root>
        <DropdownMenu.Trigger asChild>
          <Button
            className="message-menu-trigger"
            variant="ghost"
            size="icon"
            aria-label={t('messages:options', { sequence: message.sequence })}
          >
            <MoreHorizontal size={17} />
          </Button>
        </DropdownMenu.Trigger>
        <DropdownMenu.Portal>
          <DropdownMenu.Content
            className="message-menu"
            align={own ? 'end' : 'start'}
            sideOffset={5}
            collisionPadding={12}
          >
            {!message.recalled && (
              <>
                <DropdownMenu.Item
                  disabled={!canSend}
                  onSelect={() => onCompose({ mode: 'reply', ...message })}
                >
                  <Reply />
                  {t('messages:reply')}
                </DropdownMenu.Item>
                <DropdownMenu.Item
                  disabled={!canSend}
                  onSelect={() => setOperation('react')}
                >
                  <Heart />
                  {t('messages:react')}
                </DropdownMenu.Item>
                <DropdownMenu.Item onSelect={() => setOperation('forward')}>
                  <Forward />
                  {t('messages:forward')}
                </DropdownMenu.Item>
                {canEdit && (
                  <DropdownMenu.Item onSelect={() => onCompose({ mode: 'edit', ...message })}>
                    <Pencil />
                    {t('messages:edit')}
                  </DropdownMenu.Item>
                )}
                {own && canSend && (
                  <DropdownMenu.Item
                    className="menu-danger"
                    onSelect={() => setOperation('recall')}
                  >
                    <Undo2 />
                    {t('messages:recall')}
                  </DropdownMenu.Item>
                )}
                <DropdownMenu.Item onSelect={() => setOperation('history')}>
                  <History />
                  {t('messages:history')}
                </DropdownMenu.Item>
                {extraItems?.(message)}
              </>
            )}
            {onPin && !message.recalled && (
              <DropdownMenu.Item onSelect={() => setOperation('pin')}>
                {t('groups:pin')}
              </DropdownMenu.Item>
            )}
            <DropdownMenu.Item onSelect={() => setOperation('report')}>
              {t('reports:report')}
            </DropdownMenu.Item>
            <DropdownMenu.Item
              className="menu-danger"
              onSelect={() => setOperation('delete')}
            >
              <Trash2 />
              {t('messages:delete')}
            </DropdownMenu.Item>
          </DropdownMenu.Content>
        </DropdownMenu.Portal>
      </DropdownMenu.Root>
      <MessageActionDialog
        t={t}
        operation={operation}
        setOperation={setOperation}
        action={action}
        message={message}
        versions={versions}
        destination={destination}
        setDestination={setDestination}
      />
    </>
  )
}
