import { useTranslation } from "react-i18next";
import { Navigate, Outlet } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { useAuthStore } from '@/stores/auth-store';
import { usersApi } from '@/features/users/api';
import { StatePanel } from '@/components/StatePanel';
import { retryRecovery } from './providers/AuthProvider';
export function ProtectedRoute() {
  const {
    status,
    recoveryError
  } = useAuthStore();
  if (status === 'loading') return <main className="full-state"><StatePanel loading /></main>;
  if (status === 'unavailable') return <main className="full-state"><StatePanel error={recoveryError} onRetry={retryRecovery} /></main>;
  return status === 'authenticated' ? <Outlet /> : <Navigate to="/login" replace />;
}

// Ready for the later Admin slice; there is no Admin UI before the user slices pass.
export function RoleRoute({
  role
}) {
  const {
    t
  } = useTranslation();
  const profile = useQuery({
    queryKey: ['workspace', 'me'],
    queryFn: usersApi.me
  });
  if (profile.isPending) return <StatePanel loading />;
  if (profile.error) return <StatePanel error={profile.error} onRetry={profile.refetch} />;
  return profile.data.systemRole === role ? <Outlet /> : <StatePanel title={t("common:access-restricted")} description={t("common:this-area-is-not-available-to-your-account")} />;
}
