import { useMutation, useQuery } from '@tanstack/react-query'
import { Link, useNavigate } from 'react-router'
import { LogOut } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Brand } from '@/components/Brand'
import { ThemeToggle } from '@/components/ThemeToggle'
import { Avatar } from '@/components/Avatar'
import { Button } from '@/components/ui/button'
import { authApi } from '@/features/auth/api'
import { usersApi } from '@/features/users/api'
import { broadcastLogout } from '@/app/providers/AuthProvider'
import { errorMessage } from '@/lib/errors'
import { navigationItems } from './navigation'

export function ApplicationLayout({ area, title, description, children, actions }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const me = useQuery({ queryKey: ['workspace', 'me'], queryFn: usersApi.me })
  const logout = useMutation({ mutationFn: authApi.logout, onSuccess: () => { broadcastLogout(); navigate('/login', { replace: true }) } })
  const items = navigationItems(t, { area })
  return <div className="application-frame"><a className="skip-link" href="#main-content">{t('common:skipContent')}</a>
    <aside className="app-rail" aria-label={t('common:mainNavigation')}><Link className="rail-brand" to="/chat" aria-label={t('common:messages')}><Brand compact /></Link><nav className="rail-nav" aria-label={t('common:mainNavigation')}>{items.map(({ label, to, Icon, active }) => <Link key={to} className={`rail-link ${active ? 'active' : ''}`} to={to} aria-current={active ? 'page' : undefined}><Icon size={22} /><span>{label}</span></Link>)}</nav><div className="rail-bottom"><ThemeToggle /><Button variant="ghost" size="icon" className="icon-button" disabled={logout.isPending} onClick={() => logout.mutate()} aria-label={t('auth:signOut')}><LogOut /></Button><Avatar fileId={me.data?.avatarFileId} name={me.data?.displayName || '?'} small /></div></aside>
    <main className="application-main" id="main-content"><header className="page-header"><div><span className="eyebrow">MECHAT</span><h1>{title}</h1>{description && <p>{description}</p>}</div><div className="page-actions">{actions}<div className="page-session-tools"><ThemeToggle /><Button className="mobile-session-exit" variant="ghost" size="icon" disabled={logout.isPending} onClick={() => logout.mutate()} aria-label={t('auth:signOut')}><LogOut size={19} /></Button></div></div></header>{logout.error && <p role="alert" className="inline-error">{errorMessage(logout.error)}</p>}{children}</main>
    <nav className="mobile-nav" aria-label={t('common:mobileNavigation')}>{items.map(({ label, to, Icon, active }) => <Link key={to} to={to} className={active ? 'active' : ''} aria-current={active ? 'page' : undefined}><Icon size={20} /><span>{label}</span></Link>)}</nav>
  </div>
}
