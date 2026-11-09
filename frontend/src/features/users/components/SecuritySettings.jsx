import { useNavigate } from 'react-router'
import { useTranslation } from 'react-i18next'
import { ValidatedForm } from '@/components/forms/ValidatedForm'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { authApi } from '@/features/auth/api'
import { authSchemas } from '@/features/auth/schemas'
import { broadcastLogout } from '@/app/providers/AuthProvider'
import { queryClient } from '@/lib/query-client'
import { SettingsCard } from './SettingsCard'

export function SecuritySettings() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  return (
    <>
      <SettingsCard
        title={t('auth:changePasswordTitle')}
        description={t('auth:changePasswordDescription')}
      >
        <ValidatedForm
          id="change-password"
          schema={authSchemas(t).changePassword}
          fields={['currentPassword', 'newPassword', 'confirmPassword'].map((name) => ({
            name,
            label: t(`auth:fields.${name}`),
            type: 'password',
            autoComplete: name === 'currentPassword' ? 'current-password' : 'new-password',
          }))}
          onSubmit={async ({ confirmPassword: _confirm, ...input }) => {
            await authApi.changePassword(input)
            queryClient.invalidateQueries({ queryKey: ['workspace', 'sessions'] })
          }}
          submitLabel={t('auth:changePassword')}
          successMessage={t('auth:passwordChanged')}
          resetOnSuccess
        />
      </SettingsCard>
      <SettingsCard
        danger
        title={t('auth:deactivateTitle')}
        description={t('auth:deactivateDescription')}
      >
        <Dialog>
          <DialogTrigger asChild>
            <Button variant="destructive">{t('auth:deactivate')}</Button>
          </DialogTrigger>
          <DialogContent className="details-dialog">
            <DialogHeader>
              <DialogTitle>{t('auth:deactivateTitle')}</DialogTitle>
              <DialogDescription>{t('auth:deactivateConfirmation')}</DialogDescription>
            </DialogHeader>
            <ValidatedForm
              id="deactivate"
              schema={authSchemas(t).deactivate}
              fields={[
                {
                  name: 'password',
                  label: t('auth:fields.password'),
                  type: 'password',
                  autoComplete: 'current-password',
                },
              ]}
              onSubmit={async (input) => {
                await authApi.deactivate(input)
                broadcastLogout()
                navigate('/login', { replace: true })
              }}
              submitLabel={t('auth:deactivate')}
              destructive
            />
          </DialogContent>
        </Dialog>
      </SettingsCard>
    </>
  )
}
