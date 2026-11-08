import { useRef, useState } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { DropdownMenu } from 'radix-ui'
import { MoreHorizontal, Reply, Pencil, Undo2, Trash2, Forward, Heart, History } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { StatePanel } from '@/components/StatePanel'
import { queryClient } from '@/lib/query-client'
import { errorMessage } from '@/lib/errors'
import { formatDate, formatTime, formatNumber } from '@/i18n/format'
import { statusLabel } from '@/i18n'
import { messagesApi } from './api'
import { ForwardDestinations } from './ForwardDestinations';
import { ReportForm } from '@/features/moderation/ReportsPage';
import { sanitizeMessageCache } from './cache'
import { showSuccess } from '@/stores/ui-feedback-store'

const contextKey = (id) => ['workspace', 'messageContext', id]
export function MessageReply({ message }) {
  const { t } = useTranslation()
  const context = useQuery({ queryKey: contextKey(message.id), queryFn: ({ signal }) => messagesApi.context(message.id, signal), enabled: !message.recalled })
  const source = useQuery({ queryKey: ['workspace', 'message', message.replyToMessageId], queryFn: ({ signal }) => messagesApi.get(message.replyToMessageId, signal), enabled: !!message.replyToMessageId && context.data?.reply?.available && context.data?.reply?.content === 'Attachment' })
  if (!message.replyToMessageId || !context.data?.reply) return null
  const reply = context.data.reply
  // Resolve the ambiguous server sentinel before translating; user-entered "Attachment" stays intact.
  const content = reply.available ? reply.content === 'Attachment' && source.data?.content === null ? t('messages:replyAttachment') : reply.content : t('messages:replyUnavailable')
  return <blockquote className="reply-preview">{content}</blockquote>
}
export function MessageReactions({ message }) {
  const { t } = useTranslation()
  const context = useQuery({ queryKey: contextKey(message.id), queryFn: ({ signal }) => messagesApi.context(message.id, signal), enabled: !message.recalled })
  if (message.recalled) return null
  if (context.error) return <button className="metadata-retry" onClick={() => context.refetch()} title={t('messages:loadContext')}>{t('messages:contextUnavailable')}</button>
  return <div className="message-reactions">{context.data?.reactions.map((reaction) => <span key={reaction.emoji} className={context.data.myReaction === reaction.emoji ? 'mine' : ''} aria-label={t('messages:reactionLabel', { emoji: reaction.emoji, count: formatNumber(reaction.count) })}>{reaction.emoji} {formatNumber(reaction.count)}</span>)}</div>
}
export function MessageReceipt({ message, own }) {
  const { t } = useTranslation()
  const receipt = useQuery({ queryKey: ['workspace', 'receipt', message.id], queryFn: ({ signal }) => messagesApi.receipts(message.id, signal), enabled: own })
  if (!own) return null
  return receipt.data ? <span title={t('messages:receiptCounts', { delivered: formatNumber(receipt.data.deliveredCount), seen: formatNumber(receipt.data.seenCount) })}>{statusLabel(receipt.data.status)}</span> : receipt.error ? <button onClick={() => receipt.refetch()}>{t('messages:receiptUnavailable')}</button> : null
}
export function MessageActions({ message, own, onCompose, canSend = true, extraItems, onPin }) {
  const { t } = useTranslation()
  const [operation, setOperation] = useState(null)
  const [destination, setDestination] = useState('')
  const retryForward = useRef(null)
  const action = useMutation({ mutationFn: async (emoji) => {
    if (operation === 'recall') await messagesApi.recall(message.id)
    else if (operation === 'delete') await messagesApi.deleteForMe(message.id)
    else if (operation === 'pin') await onPin(message.id)
    else if (operation === 'forward') {
      if (!retryForward.current || retryForward.current.destination !== destination) retryForward.current = { destination, clientMessageId: crypto.randomUUID() }
      await messagesApi.forward(message.id, destination, retryForward.current.clientMessageId)
    } else if (emoji) await messagesApi.react(message.id, emoji)
    else await messagesApi.removeReaction(message.id)
    if (operation === 'recall' || operation === 'delete') sanitizeMessageCache(message.id, operation === 'recall')
    showSuccess(t('messages:actionSaved'))
    await queryClient.invalidateQueries({ queryKey: ['workspace'] })
    setOperation(null)
  } })
  const versions = useQuery({ queryKey: ['workspace', 'editHistory', message.id], queryFn: ({ signal }) => messagesApi.history(message.id, signal), enabled: operation === 'history' })
  const canEdit = own && canSend && message.content && Date.now() >= new Date(message.createdAt).getTime() && Date.now() - new Date(message.createdAt).getTime() <= 15 * 60_000
  return <><DropdownMenu.Root><DropdownMenu.Trigger asChild><Button className="message-menu-trigger" variant="ghost" size="icon" aria-label={t('messages:options', { sequence: message.sequence })}><MoreHorizontal size={17} /></Button></DropdownMenu.Trigger><DropdownMenu.Portal><DropdownMenu.Content className="message-menu" align={own ? 'end' : 'start'} sideOffset={5} collisionPadding={12}>
    {!message.recalled && <><DropdownMenu.Item disabled={!canSend} onSelect={() => onCompose({ mode: 'reply', ...message })}><Reply />{t('messages:reply')}</DropdownMenu.Item><DropdownMenu.Item disabled={!canSend} onSelect={() => setOperation('react')}><Heart />{t('messages:react')}</DropdownMenu.Item><DropdownMenu.Item onSelect={() => setOperation('forward')}><Forward />{t('messages:forward')}</DropdownMenu.Item>{canEdit && <DropdownMenu.Item onSelect={() => onCompose({ mode: 'edit', ...message })}><Pencil />{t('messages:edit')}</DropdownMenu.Item>}{own && canSend && <DropdownMenu.Item className="menu-danger" onSelect={() => setOperation('recall')}><Undo2 />{t('messages:recall')}</DropdownMenu.Item>}<DropdownMenu.Item onSelect={() => setOperation('history')}><History />{t('messages:history')}</DropdownMenu.Item>{extraItems?.(message)}</>}
    {onPin && !message.recalled && <DropdownMenu.Item onSelect={() => setOperation('pin')}>{t('groups:pin')}</DropdownMenu.Item>}
    <DropdownMenu.Item onSelect={() => setOperation('report')}>{t('reports:report')}</DropdownMenu.Item>
    <DropdownMenu.Item className="menu-danger" onSelect={() => setOperation('delete')}><Trash2 />{t('messages:delete')}</DropdownMenu.Item>
  </DropdownMenu.Content></DropdownMenu.Portal></DropdownMenu.Root>
  <Dialog open={!!operation} onOpenChange={(open) => { if (!open && !action.isPending) { setOperation(null); action.reset() } }}><DialogContent className="details-dialog"><DialogHeader><DialogTitle>{t(operation === 'report' ? 'reports:report' : operation === 'pin' ? 'groups:pin' : `messages:${operation || 'options'}`)}</DialogTitle><DialogDescription>{t(operation === 'report' ? 'reports:reportDescription' : operation === 'pin' ? 'groups:pinsDescription' : operation === 'recall' ? 'messages:recallDescription' : operation === 'delete' ? 'messages:deleteDescription' : operation === 'forward' ? 'messages:forwardDescription' : operation === 'react' ? 'messages:reactionDescription' : 'messages:history')}</DialogDescription></DialogHeader>
    {action.error && <p role="alert" className="form-feedback feedback-error">{errorMessage(action.error)}</p>}
    {operation === 'report' ? <ReportForm targetType="Message" targetId={message.id} /> : operation === 'history' ? versions.isPending ? <StatePanel loading /> : versions.error ? <StatePanel error={versions.error} onRetry={versions.refetch} /> : versions.data.versions.length === 0 ? <StatePanel title={t('messages:noHistory')} /> : <ol className="message-versions">{versions.data.versions.map((version) => <li key={version.version}><strong>{t('messages:version', { version: version.version, date: `${formatDate(version.editedAt)} · ${formatTime(version.editedAt)}` })}</strong><p>{version.content}</p></li>)}</ol> : operation === 'react' ? <div className="reaction-picker">{['👍', '❤️', '😂', '😮', '😢', '🎉'].map((emoji) => <Button key={emoji} variant="outline" disabled={action.isPending} onClick={() => action.mutate(emoji)} aria-label={t('messages:reactionLabel', { emoji, count: 1 })}>{emoji}</Button>)}<Button variant="ghost" disabled={action.isPending} onClick={() => action.mutate(null)}>{t('messages:removeReaction')}</Button></div> : <>
      {operation === 'forward' && <ForwardDestinations id={`forward-${message.id}`} value={destination} onChange={setDestination} disabled={action.isPending} />}

      <div className="confirmation-actions"><Button variant="outline" disabled={action.isPending} onClick={() => setOperation(null)}>{t('common:cancel')}</Button><Button variant={operation === 'recall' || operation === 'delete' ? 'destructive' : 'default'} disabled={action.isPending || operation === 'forward' && !destination} onClick={() => action.mutate()}>{action.isPending ? t('common:saving') : t('common:confirm')}</Button></div>
    </>}
  </DialogContent></Dialog></>
}
