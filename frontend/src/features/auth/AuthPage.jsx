import { Link } from 'react-router'
import { useTranslation } from 'react-i18next'
import { Brand } from '@/components/Brand'
import { ThemeToggle } from '@/components/ThemeToggle'
import { ValidatedForm } from '@/components/forms/ValidatedForm'
import { authSchemas } from './schemas'
import { authApi } from './api'

export const AUTH_ROUTES = {
  register: '/register', verifyEmail: '/verify-email', resendVerification: '/resend-verification',
  forgotPassword: '/forgot-password', resetPassword: '/reset-password', reactivate: '/reactivate',
}
export function AuthPage({ mode }) {
  const { t } = useTranslation()
  const field = (name, options = {}) => ({ name, label: t(`auth:fields.${name}`), ...options })
  const fields = {
    register: [field('email', { type: 'email', autoComplete: 'email' }), field('username', { autoComplete: 'username', maxLength: 50 }), field('password', { type: 'password', autoComplete: 'new-password' }), field('confirmPassword', { type: 'password', autoComplete: 'new-password' })],
    verifyEmail: [field('token', { autoComplete: 'off', helper: t('auth:tokenHelp') })],
    resendVerification: [field('email', { type: 'email', autoComplete: 'email' })],
    forgotPassword: [field('email', { type: 'email', autoComplete: 'email' })],
    resetPassword: [field('token', { autoComplete: 'off', helper: t('auth:tokenHelp') }), field('newPassword', { type: 'password', autoComplete: 'new-password' }), field('confirmPassword', { type: 'password', autoComplete: 'new-password' })],
    reactivate: [field('identifier', { autoComplete: 'username' }), field('password', { type: 'password', autoComplete: 'current-password' })],
  }
  async function submit(values) {
    // Confirmation is client-only; request DTO field names and values remain unchanged.
    const payload = { ...values }
    delete payload.confirmPassword
    await authApi[mode](payload)
  }
  return <main className="auth-flow-page" id="main-content"><header className="auth-flow-top"><Link to="/login" aria-label={t('auth:brandBackToLogin')}><Brand /></Link><ThemeToggle /></header>
    <section className="auth-flow-card" aria-labelledby="auth-flow-title"><span className="eyebrow">MECHAT</span><h1 id="auth-flow-title">{t(`auth:pages.${mode}.title`)}</h1><p className="page-description">{t(`auth:pages.${mode}.description`)}</p>
      <ValidatedForm key={mode} id={mode} fields={fields[mode]} schema={authSchemas(t)[mode]} onSubmit={submit} submitLabel={t(`auth:pages.${mode}.submit`)} successMessage={t(`auth:pages.${mode}.success`)} resetOnSuccess />
      <nav className="auth-flow-links" aria-label={t('auth:accountNavigation')}><Link to="/login">{t('auth:backToLogin')}</Link>{Object.entries(AUTH_ROUTES).filter(([key]) => key !== mode).map(([key, route]) => <Link key={key} to={route}>{t(`auth:pages.${key}.link`)}</Link>)}</nav>
    </section><footer className="auth-flow-footer">{t('auth:securityNote')}</footer>
  </main>
}
