import { translate } from "@/i18n";
import { useTranslation } from "react-i18next";
import { lazy, Suspense } from 'react';
import { createBrowserRouter, Link, Navigate, Outlet } from 'react-router';
import { AuthProvider } from './providers/AuthProvider';
import { ProtectedRoute, RoleRoute } from './guards';
import { StatePanel } from '@/components/StatePanel';
import { useAuthStore } from '@/stores/auth-store';
const LoginPage = lazy(() => import('@/features/auth/LoginPage').then(module => ({
  default: module.LoginPage
})));
const WorkspacePage = lazy(() => import('@/features/conversations/WorkspacePage').then(module => ({
  default: module.WorkspacePage
})));
const RealtimeProvider = lazy(() => import('./providers/RealtimeProvider').then(module => ({
  default: module.RealtimeProvider
})));
const AuthPage = lazy(() => import('@/features/auth/AuthPage').then(module => ({ default: module.AuthPage })));
const AccountPage = lazy(() => import('@/features/users/AccountPage').then(module => ({ default: module.AccountPage })));
const FriendsPage = lazy(() => import('@/features/friends/FriendsPage').then(module => ({ default: module.FriendsPage })));
const GroupCreatePage = lazy(() => import('@/features/groups/GroupPages').then(module => ({ default: module.GroupCreatePage })));
const GroupInvitationsPage = lazy(() => import('@/features/groups/GroupPages').then(module => ({ default: module.GroupInvitationsPage })));
const GroupManagementPage = lazy(() => import('@/features/groups/GroupPages').then(module => ({ default: module.GroupManagementPage })));
const GroupJoinPage = lazy(() => import('@/features/groups/GroupPages').then(module => ({ default: module.GroupJoinPage })));
const NotificationsPage = lazy(() => import('@/features/notifications/NotificationsPage').then(module => ({ default: module.NotificationsPage })));
const ReportsPage = lazy(() => import('@/features/moderation/ReportsPage').then(module => ({ default: module.ReportsPage })));
const AppealsPage = lazy(() => import('@/features/moderation/AppealsPage').then(module => ({ default: module.AppealsPage })));
const AdminPage = lazy(() => import('@/features/admin/AdminPage').then(module => ({ default: module.AdminPage })));
function LazyScreen({
  children
}) {
  return <Suspense fallback={<StatePanel loading />}>{children}</Suspense>;
}
function AuthenticatedLayout() {
  return <LazyScreen><RealtimeProvider><Outlet /></RealtimeProvider></LazyScreen>;
}
function LoginRoute() {
  const status = useAuthStore(state => state.status);
  if (status === 'loading') return <div className="full-state"><StatePanel loading /></div>;
  return status === 'authenticated' ? <Navigate to="/chat" replace /> : <LazyScreen><LoginPage /></LazyScreen>;
}
function NotFound() {
  const {
    t
  } = useTranslation();
  return <main className="full-state"><StatePanel title={t("common:nothing-here-just-yet")} description={t("common:this-page-isn-t-available")} /><Link to="/chat" className="text-link">{t("common:back-to-mechat")}</Link></main>;
}
const routes = [{
  element: <AuthProvider><Outlet /></AuthProvider>,
  errorElement: <main className="full-state"><StatePanel title={translate("common:something-interrupted-mechat")} description={translate("common:reload-the-page-to-reconnect")} /><a className="text-link" href="/login">{translate("common:reload-mechat")}</a></main>,
  children: [{
    path: '/login',
    element: <LoginRoute />
  }, {
    path: '/appeals', element: <LazyScreen><AppealsPage /></LazyScreen>
  }, ...Object.entries({ register: '/register', verifyEmail: '/verify-email', resendVerification: '/resend-verification', forgotPassword: '/forgot-password', resetPassword: '/reset-password', reactivate: '/reactivate' }).map(([mode, path]) => ({ path, element: <LazyScreen><AuthPage key={mode} mode={mode} /></LazyScreen> })), {
    element: <ProtectedRoute />,
    children: [{
      element: <AuthenticatedLayout />,
      children: [{
        path: '/',
        element: <Navigate to="/chat" replace />
      }, {
        path: '/chat/:id?',
        element: <LazyScreen><WorkspacePage key="private" /></LazyScreen>
      }, {
        path: '/groups/:id?',
        element: <LazyScreen><WorkspacePage key="group" area="group" /></LazyScreen>
      }, {
        path: '/account/:section?',
        element: <LazyScreen><AccountPage /></LazyScreen>
      }, {
        path: '/friends/:section?',
        element: <LazyScreen><FriendsPage /></LazyScreen>
      }, {
        path: '/message-requests/:id?',
        element: <LazyScreen><WorkspacePage key="requests" requestMode /></LazyScreen>
      }, {
        path: '/groups/new', element: <LazyScreen><GroupCreatePage /></LazyScreen>
      }, {
        path: '/groups/invitations', element: <LazyScreen><GroupInvitationsPage /></LazyScreen>
      }, {
        path: '/groups/join', element: <LazyScreen><GroupJoinPage /></LazyScreen>
      }, {
        path: '/groups/:id/manage', element: <LazyScreen><GroupManagementPage /></LazyScreen>
      }, {
        path: '/notifications', element: <LazyScreen><NotificationsPage /></LazyScreen>
      }, {
        path: '/reports', element: <LazyScreen><ReportsPage /></LazyScreen>
      }, {
        element: <RoleRoute role="Admin" />, children: [{ path: '/admin/:section?/:id?', element: <LazyScreen><AdminPage /></LazyScreen> }]
      }]
    }]
  }, {
    path: '*',
    element: <NotFound />
  }]
}];
if (import.meta.env.DEV) {
  const DesignReview = lazy(() => import('@/design/DesignReview'));
  for (const screen of ['login', 'private', 'group']) routes.unshift({
    path: `/design/${screen}`,
    element: <Suspense fallback={<StatePanel loading />}><DesignReview screen={screen} /></Suspense>
  });
}
export const router = createBrowserRouter(routes);
