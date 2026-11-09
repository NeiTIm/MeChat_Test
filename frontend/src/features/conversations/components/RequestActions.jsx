import { useTranslation } from 'react-i18next'
import { queryClient } from '@/lib/query-client'
import { requestsApi } from '@/features/messages/api'
import { ConfirmAction } from '@/components/ConfirmAction'

export function RequestActions({ request, onDone }) {
  const { t } = useTranslation()
  const respond = async (method) => {
    await requestsApi[method](request.id)
    await queryClient.resetQueries({ queryKey: ['workspace'] })
    onDone(method === 'accept')
  }
  return (
    <div className="request-actions">
      {['accept', 'reject', 'block'].map((method) => (
        <ConfirmAction
          key={method}
          label={t(`messages:request${method[0].toUpperCase()}${method.slice(1)}`)}
          description={t(`messages:request${method[0].toUpperCase()}${method.slice(1)}Description`)}
          destructive={method === 'block'}
          onConfirm={() => respond(method)}
        />
      ))}
    </div>
  )
}
