import { ProfileSettings } from '@/features/users/components/ProfileSettings'
import { PrivacySettings } from '@/features/users/components/PrivacySettings'
import { SecuritySettings } from '@/features/users/components/SecuritySettings'
import { SessionSettings } from '@/features/users/components/SessionSettings'
import { Link, NavLink, useParams } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { ApplicationLayout } from '@/components/layout/ApplicationLayout'
import { StatePanel } from '@/components/StatePanel'
import { usersApi } from './api'
import { InterfacePreferences } from './InterfacePreferences'

const tabs = ['profile', 'privacy', 'security', 'sessions', 'interface']
export function AccountPage() {
  const { t } = useTranslation()
  const { section = 'profile' } = useParams()
  const me = useQuery({ queryKey: ['workspace', 'me'], queryFn: usersApi.me })
  return (
    <ApplicationLayout
      area="account"
      title={t('users:accountTitle')}
      description={t('users:accountDescription')}
    >
      <div className="settings-layout">
        <nav
          className="settings-tabs"
          aria-label={t('users:settingsNavigation')}
        >
          {tabs.map((tab) => (
            <NavLink
              to={`/account/${tab}`}
              key={tab}
              className={section === tab ? 'active' : ''}
            >
              {t(`users:tabs.${tab}`)}
            </NavLink>
          ))}
        </nav>
        <section className="settings-content">
          {me.isPending ? (
            <StatePanel loading />
          ) : me.error ? (
            <StatePanel
              error={me.error}
              onRetry={me.refetch}
            />
          ) : section === 'profile' ? (
            <ProfileSettings profile={me.data} />
          ) : section === 'privacy' ? (
            <PrivacySettings />
          ) : section === 'security' ? (
            <SecuritySettings />
          ) : section === 'sessions' ? (
            <SessionSettings />
          ) : section === 'interface' ? (
            <InterfacePreferences />
          ) : (
            <StatePanel
              title={t('common:nothing-here-just-yet')}
              description={t('users:invalidSection')}
            />
          )}
        </section>
      </div>
      <div className="account-service-links">
        <Link to="/reports">{t('reports:title')}</Link>
        <Link to="/appeals">{t('appeals:title')}</Link>
        {me.data?.systemRole === 'Admin' && <Link to="/admin">{t('common:admin')}</Link>}
      </div>
    </ApplicationLayout>
  )
}
