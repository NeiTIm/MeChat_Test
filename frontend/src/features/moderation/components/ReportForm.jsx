import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { ValidatedForm } from '@/components/forms/ValidatedForm'
import { queryClient } from '@/lib/query-client'
import { reportsApi } from '@/features/moderation/api'

export function ReportForm({ targetType, targetId }) {
  const { t } = useTranslation()
  return (
    <ValidatedForm
      id={`report-${targetId}`}
      fields={[{ name: 'reason', label: t('reports:reason'), type: 'textarea', maxLength: 1000 }]}
      schema={z.object({
        reason: z.string().trim().min(1, t('common:required')).max(1000, t('reports:reasonLimit')),
      })}
      onSubmit={async ({ reason }) => {
        await reportsApi.create({ targetType, targetId, reason })
        await queryClient.invalidateQueries({ queryKey: ['workspace', 'reports'] })
      }}
      submitLabel={t('reports:submit')}
      successMessage={t('reports:submitted')}
      resetOnSuccess
    />
  )
}
