import { useTranslation } from 'react-i18next'
import { useAuthStore } from '@/stores/auth-store'
import { setToastEnabled, toastEnabled, useUiFeedbackStore } from '@/stores/ui-feedback-store'

export function InterfacePreferences() {
  const { t } = useTranslation()
  const userId = useAuthStore((state) => state.access?.userId)
  useUiFeedbackStore((state) => state.preferences)
  const enabled = toastEnabled(userId)
  return <section className="settings-card"><h2>{t('users:interfaceTitle')}</h2><p className="page-description">{t('users:interfaceLocal')}</p>
    <label className="privacy-row"><span><strong>{t('users:toastTitle')}</strong><small>{t('users:toastDescription')}</small></span><input type="checkbox" checked={enabled} onChange={(event) => setToastEnabled(userId, event.target.checked)} /></label>
    <p className="field-help" role="status">{t(enabled ? 'users:toastOn' : 'users:toastOff')}</p>
  </section>
}
