import { useNavigate } from 'react-router'
import { useTranslation } from 'react-i18next'
import { ConfirmAction } from '@/components/ConfirmAction'
import { queryClient } from '@/lib/query-client'
import { groupsApi } from '@/features/groups/api'

export function GroupDanger({ group }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  async function act(method) {
    await groupsApi[method](group.id)
    navigate('/groups')
    await queryClient.resetQueries({ queryKey: ['workspace'] })
  }
  return (
    <section className="settings-card danger-card">
      <h2>{t('groups:membershipTitle')}</h2>
      <p className="page-description">
        {t(group.myRole === 'Owner' ? 'groups:ownerLeaveHint' : 'groups:leaveDescription')}
      </p>
      <div className="section-actions">
        {group.myRole !== 'Owner' && (
          <ConfirmAction
            label={t('groups:leave')}
            description={t('groups:leaveDescription')}
            destructive
            onConfirm={() => act('leave')}
          />
        )}
        {group.myRole === 'Owner' && (
          <ConfirmAction
            label={t('groups:dissolve')}
            description={t('groups:dissolveDescription')}
            destructive
            onConfirm={() => act('dissolve')}
          />
        )}
      </div>
    </section>
  )
}
