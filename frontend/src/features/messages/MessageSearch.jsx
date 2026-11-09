import { useState } from 'react'
import { useInfiniteQuery, useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Search } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { StatePanel } from '@/components/StatePanel'
import { formatDate, formatTime } from '@/i18n/format'
import { messagesApi } from './api'

export function MessageSearch({ conversationId, onJump }) {
  const { t } = useTranslation()
  const [input, setInput] = useState('')
  const [query, setQuery] = useState('')
  const [open, setOpen] = useState(false)
  const results = useInfiniteQuery({
    queryKey: ['workspace', 'messageSearch', conversationId, query],
    queryFn: ({ pageParam, signal }) =>
      messagesApi.search(conversationId, query, pageParam, signal),
    initialPageParam: undefined,
    getNextPageParam: (page) => (page.hasMore ? page.nextCursor : undefined),
    enabled: open && query.length >= 2,
  })
  const messages = [
    ...new Map(
      (results.data?.pages || []).flatMap((page) => page.items).map((item) => [item.id, item]),
    ).values(),
  ]
  return (
    <Dialog
      open={open}
      onOpenChange={setOpen}
    >
      <DialogTrigger asChild>
        <Button variant="outline">
          <Search size={17} />
          {t('messages:searchTitle')}
        </Button>
      </DialogTrigger>
      <DialogContent className="details-dialog">
        <DialogHeader>
          <DialogTitle>{t('messages:searchTitle')}</DialogTitle>
          <DialogDescription>{t('messages:searchDescription')}</DialogDescription>
        </DialogHeader>
        <form
          className="dialog-search"
          onSubmit={(event) => {
            event.preventDefault()
            if (input.trim().length >= 2) setQuery(input.trim())
          }}
        >
          <label htmlFor="message-query">{t('messages:searchLabel')}</label>
          <Input
            id="message-query"
            placeholder={t('messages:searchPlaceholder')}
            value={input}
            onChange={(event) => setInput(event.target.value)}
            maxLength={100}
          />
          <Button
            type="submit"
            disabled={input.trim().length < 2}
          >
            {t('messages:searchSubmit')}
          </Button>
        </form>
        {!query ? (
          <p className="field-help">{t('messages:searchHint')}</p>
        ) : results.isPending ? (
          <StatePanel loading />
        ) : results.error ? (
          <StatePanel
            error={results.error}
            onRetry={results.refetch}
          />
        ) : (
          <div className="message-search-results">
            {messages.length === 0 ? (
              <StatePanel title={t('messages:searchEmpty')} />
            ) : (
              messages.map((message) => (
                <article key={message.id}>
                  <time>
                    {formatDate(message.createdAt)} · {formatTime(message.createdAt)}
                  </time>
                  <p>{message.content}</p>
                  <MessageDetail
                    id={message.id}
                    onJump={
                      onJump
                        ? (message) => {
                            setOpen(false)
                            onJump(message)
                          }
                        : undefined
                    }
                  />
                </article>
              ))
            )}
            {results.hasNextPage && (
              <Button
                variant="outline"
                disabled={results.isFetchingNextPage}
                onClick={() => results.fetchNextPage()}
              >
                {results.isFetchingNextPage ? t('common:saving') : t('messages:more')}
              </Button>
            )}
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
function MessageDetail({ id, onJump }) {
  const { t } = useTranslation()
  const [open, setOpen] = useState(false)
  const message = useQuery({
    queryKey: ['workspace', 'message', id],
    queryFn: ({ signal }) => messagesApi.get(id, signal),
    enabled: open,
  })
  return (
    <Dialog
      open={open}
      onOpenChange={setOpen}
    >
      <DialogTrigger asChild>
        <Button variant="link">{t('messages:viewMessage')}</Button>
      </DialogTrigger>
      <DialogContent className="details-dialog">
        <DialogHeader>
          <DialogTitle>{t('messages:messageTitle')}</DialogTitle>
          <DialogDescription>{t('messages:searchDescription')}</DialogDescription>
        </DialogHeader>
        {message.isPending ? (
          <StatePanel loading />
        ) : message.error ? (
          <StatePanel
            error={message.error}
            onRetry={message.refetch}
          />
        ) : (
          <>
            <p className="message-detail-content">
              {message.data.recalled ? t('chat:this-message-was-recalled') : message.data.content}
            </p>
            {onJump &&
              Number.isSafeInteger(message.data.sequence) &&
              message.data.sequence > 0 &&
              !message.data.recalled && (
                <Button
                  variant="outline"
                  onClick={() => {
                    setOpen(false)
                    onJump(message.data)
                  }}
                >
                  {t('messages:jumpToMessage')}
                </Button>
              )}
          </>
        )}
      </DialogContent>
    </Dialog>
  )
}
