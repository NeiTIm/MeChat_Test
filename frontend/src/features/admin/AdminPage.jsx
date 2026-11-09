import { AdminDashboard } from '@/features/admin/components/AdminDashboard'
import { AdminList } from '@/features/admin/components/AdminList'
import { AdminCase } from '@/features/admin/components/AdminCase'
import { AdminRetention } from '@/features/admin/components/AdminRetention'
import { NavLink, useParams } from 'react-router'
import { useTranslation } from 'react-i18next'
import { ApplicationLayout } from '@/components/layout/ApplicationLayout'
import { StatePanel } from '@/components/StatePanel'

const sections = ['dashboard', 'users', 'groups', 'reports', 'appeals', 'audit', 'retention']
export function AdminPage() {
  const { t } = useTranslation()
  const { section = 'dashboard', id } = useParams()
  return (
    <ApplicationLayout
      area="account"
      title={t('admin:title')}
      description={t('admin:description')}
    >
      <nav
        className="admin-tabs"
        aria-label={t('admin:navigation')}
      >
        {sections.map((tab) => (
          <NavLink
            to={tab === 'dashboard' ? '/admin' : `/admin/${tab}`}
            key={tab}
            className={section === tab ? 'active' : ''}
          >
            {t(`admin:tabs.${tab}`)}
          </NavLink>
        ))}
      </nav>
      {section === 'dashboard' ? (
        <AdminDashboard />
      ) : section === 'retention' ? (
        <AdminRetention />
      ) : section === 'reports' && id ? (
        <AdminCase id={id} />
      ) : sections.includes(section) ? (
        <AdminList
          key={section}
          section={section}
        />
      ) : (
        <StatePanel title={t('common:nothing-here-just-yet')} />
      )}
    </ApplicationLayout>
  )
}
