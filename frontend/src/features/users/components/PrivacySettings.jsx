import { useState } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { StatePanel } from '@/components/StatePanel'
import { Button } from '@/components/ui/button'
import { queryClient } from '@/lib/query-client'
import { errorMessage } from '@/lib/errors'
import { usersApi } from '@/features/users/api'
import { showSuccess } from '@/stores/ui-feedback-store'
import { SettingsCard } from './SettingsCard'

export function PrivacySettings() {
  const privacy = useQuery({ queryKey: ['workspace', 'privacy'], queryFn: usersApi.privacy })
  if (privacy.isPending) return <StatePanel loading />
  if (privacy.error)
    return (
      <StatePanel
        error={privacy.error}
        onRetry={privacy.refetch}
      />
    )
  return <PrivacyForm initial={privacy.data} />
}
function PrivacyForm({ initial }) {
  const { t } = useTranslation()
  const [values, setValues] = useState(initial)
  const mutation = useMutation({
    mutationFn: usersApi.updatePrivacy,
    onSuccess: () => {
      showSuccess(t('users:privacySaved'))
      return queryClient.invalidateQueries({ queryKey: ['workspace'] })
    },
  })
  return (
    <SettingsCard
      title={t('users:privacyTitle')}
      description={t('users:privacyDescription')}
    >
      <form
        onSubmit={(event) => {
          event.preventDefault()
          mutation.mutate(values)
        }}
      >
        <fieldset
          className="login-fields"
          disabled={mutation.isPending}
        >
          {['showOnline', 'showLastSeen', 'allowStrangerMessages', 'allowGroupInvites'].map(
            (name) => (
              <label
                className="privacy-row"
                key={name}
              >
                <span>
                  <strong>{t(`users:privacy.${name}.title`)}</strong>
                  <small>{t(`users:privacy.${name}.description`)}</small>
                </span>
                <input
                  type="checkbox"
                  checked={!!values[name]}
                  onChange={(event) => setValues({ ...values, [name]: event.target.checked })}
                />
              </label>
            ),
          )}
          <Button
            type="submit"
            disabled={mutation.isPending}
          >
            {mutation.isPending ? t('common:saving') : t('common:save')}
          </Button>
        </fieldset>
        {mutation.error && (
          <p
            className="form-feedback feedback-error"
            role="alert"
          >
            {errorMessage(mutation.error)}
          </p>
        )}
        {mutation.isSuccess && (
          <p
            className="form-feedback feedback-success"
            role="status"
          >
            {t('users:privacySaved')}
          </p>
        )}
      </form>
    </SettingsCard>
  )
}
