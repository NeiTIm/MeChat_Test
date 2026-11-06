import { useState } from 'react'
import { Link, NavLink, useNavigate, useParams } from 'react-router'
import { useInfiniteQuery, useMutation, useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { BarChart3, Download, Flag, MessageSquare, ShieldCheck, Users } from 'lucide-react'
import { ApplicationLayout } from '@/components/layout/ApplicationLayout'
import { StatePanel } from '@/components/StatePanel'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { roleLabel, statusLabel } from '@/i18n'
import { formatDate, formatTime, formatNumber } from '@/i18n/format'
import { errorMessage } from '@/lib/errors'
import { useAuthStore } from '@/stores/auth-store'
import { queryClient } from '@/lib/query-client'
import { AdminAction, HoldAction } from './AdminActions'
import { adminApi } from './api'
import { auditDetails } from './format'

const sections = ['dashboard', 'users', 'groups', 'reports', 'appeals', 'audit', 'retention']
export function AdminPage() {
  const { t } = useTranslation()
  const { section = 'dashboard', id } = useParams()
  return <ApplicationLayout area="account" title={t('admin:title')} description={t('admin:description')}><nav className="admin-tabs" aria-label={t('admin:navigation')}>{sections.map((tab) => <NavLink to={tab === 'dashboard' ? '/admin' : `/admin/${tab}`} key={tab} className={section === tab ? 'active' : ''}>{t(`admin:tabs.${tab}`)}</NavLink>)}</nav>
    {section === 'dashboard' ? <AdminDashboard /> : section === 'retention' ? <AdminRetention /> : section === 'reports' && id ? <AdminCase id={id} /> : sections.includes(section) ? <AdminList key={section} section={section} /> : <StatePanel title={t('common:nothing-here-just-yet')} />}
  </ApplicationLayout>
}
function MetricCards({ data, namespace = 'admin:metrics' }) {
  const { t } = useTranslation()
  const icons = [Users, MessageSquare, BarChart3, Flag]
  return <div className="admin-metrics">{Object.entries(data).map(([key, value], index) => { const Icon = icons[index % icons.length]; return <article key={key}><span className="metric-icon"><Icon size={21} /></span><p>{t(`${namespace}.${key}`)}</p><strong>{formatNumber(value)}</strong></article> })}</div>
}
function AdminDashboard() {
  const { t } = useTranslation()
  const dashboard = useQuery({ queryKey: ['workspace', 'admin', 'dashboard'], queryFn: adminApi.dashboard })
  if (dashboard.isPending) return <StatePanel loading />
  if (dashboard.error) return <StatePanel error={dashboard.error} onRetry={dashboard.refetch} />
  const { activity, ...metrics } = dashboard.data
  const max = Math.max(1, ...activity.map((day) => day.messages))
  return <><MetricCards data={metrics} /><section className="settings-card admin-activity"><h2>{t('admin:activityTitle')}</h2><p className="page-description">{t('admin:activityDescription')}</p>{!activity.length ? <StatePanel title={t('admin:empty')} /> : <div className="activity-rows"><div className="activity-row activity-labels"><span>{t('admin:date')}</span><span>{t('admin:newUsers')}</span><span>{t('admin:messages')}</span></div>{activity.map((day) => <div className="activity-row" key={day.date}><time>{formatDate(day.date)}</time><strong>{formatNumber(day.newUsers)}</strong><span className="activity-value"><span className="activity-bar" style={{ width: `${Math.max(1, day.messages / max * 100)}%` }} /><strong>{formatNumber(day.messages)}</strong></span></div>)}</div>}</section></>
}
function AdminList({ section }) {
  const { t } = useTranslation()
  const [filter, setFilter] = useState('')
  const records = useInfiniteQuery({ queryKey: ['workspace', 'admin', section], queryFn: ({ pageParam, signal }) => adminApi.list(section, pageParam, signal), initialPageParam: 0, getNextPageParam: (last, pages) => last.length === 100 ? pages.reduce((count, page) => count + page.length, 0) : undefined })
  const items = [...new Map((records.data?.pages || []).flat().map((item) => [item.id, item])).values()]
  const matching = items.filter((row) => [row.username, row.email, row.name, row.reason, row.action, row.id].filter(Boolean).some((value) => value.toLowerCase().includes(filter.trim().toLowerCase())))
  return <section className="admin-records"><div className="admin-list-tools"><div><label htmlFor="admin-filter">{t('admin:searchLoaded')}</label><Input id="admin-filter" value={filter} onChange={(event) => setFilter(event.target.value)} placeholder={t('admin:searchPlaceholder')} /></div><span>{t('admin:loaded', { count: formatNumber(items.length) })}</span></div>
    {records.isPending ? <StatePanel loading /> : records.error ? <StatePanel error={records.error} onRetry={records.refetch} /> : !matching.length ? <div className="settings-card"><StatePanel title={t('admin:empty')} /></div> : <div className="admin-record-list">{matching.map((item) => <AdminRecord key={item.id} section={section} item={item} />)}</div>}{records.hasNextPage && <Button variant="outline" className="load-more-people" disabled={records.isFetchingNextPage} onClick={() => records.fetchNextPage()}>{records.isFetchingNextPage ? t('common:saving') : t('admin:more')}</Button>}
  </section>
}
function AdminRecord({ section, item }) {
  const { t } = useTranslation()
  const ownId = useAuthStore((state) => state.access?.userId)
  const navigate = useNavigate()
  const claim = useMutation({ mutationFn: () => adminApi.claim(item.id), onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['workspace', 'admin'] }); navigate(`/admin/reports/${item.id}`) } })
  const action = (method, label, options = {}) => <AdminAction key={method} label={t(label)} target={item.username || item.name || item.id} onConfirm={(values) => adminApi[method](item.id, values)} {...options} />
  return <article className="admin-record"><div className="admin-record-content">
    {section === 'users' ? <><header><h2>@{item.username}</h2><span className="role-chip">{roleLabel(item.systemRole)}</span><span className={`admin-status ${item.suspended ? 'suspended' : ''}`}>{t(item.suspended ? 'admin:suspended' : 'admin:active')}</span></header><p>{item.email}</p><p className="field-help">{t('admin:created')}: {formatDate(item.createdAt)}</p></> : section === 'groups' ? <><header><h2>{item.name}</h2><span className="role-chip">{statusLabel(item.status)}</span></header><p>{t('admin:memberCount', { count: formatNumber(item.memberCount) })}</p></> : section === 'reports' ? <><header><h2>{t(`reports:targets.${item.targetType}`)}</h2><span className="role-chip">{statusLabel(item.status)}</span></header><p>{item.reason}</p><p className="field-help">{formatDate(item.createdAt)} · <span className="record-id">{item.targetId}</span></p></> : section === 'appeals' ? <><header><h2>{item.username || item.userId}</h2><span className="role-chip">{statusLabel(item.status)}</span></header><p>{item.reason}</p><p className="field-help">{formatDate(item.createdAt)}</p>{item.reviewNote && <p>{item.reviewNote}</p>}</> : <><header><h2>{t(`admin:auditActions.${item.action}`, { defaultValue: t('admin:unknownAction') })}</h2><span className="role-chip">{t(item.legalHold ? 'admin:held' : 'admin:notHeld')}</span></header><p className="field-help">{formatDate(item.createdAt)} · {formatTime(item.createdAt)}</p><dl className="audit-details"><div><dt>{t('admin:auditActor')}</dt><dd>{item.adminUserId}</dd></div><div><dt>{t('admin:auditTarget')}</dt><dd>{t(`admin:targetTypes.${item.targetType}`, { defaultValue: t('admin:unknownTarget') })} · {item.targetId}</dd></div>{item.details && <div><dt>{t('admin:auditDetails')}</dt><dd>{auditDetails(item)}</dd></div>}</dl></>}
  </div><div className="admin-record-actions">
    {section === 'users' && item.id !== ownId && <>{action('warn', 'admin:warn')}{item.systemRole !== 'Admin' && (item.suspended ? action('unsuspend', 'admin:unsuspend') : action('suspend', 'admin:suspend', { destructive: true, suspension: true }))}</>}
    {section === 'groups' && item.status === 'Active' && action('disableGroup', 'admin:disableGroup', { destructive: true })}
    {section === 'reports' && <>{item.reviewedByAdminId === ownId ? <Link className="text-link" to={`/admin/reports/${item.id}`}>{t('admin:viewCase')}</Link> : !item.reviewedByAdminId && item.status === 'Pending' ? <Button variant="outline" disabled={claim.isPending} onClick={() => claim.mutate()}>{claim.isPending ? t('common:saving') : t('admin:claim')}</Button> : <span className="field-help">{t('admin:assignedOther')}</span>}{claim.error && <p role="alert" className="field-error">{errorMessage(claim.error)}</p>}</>}
    {section === 'appeals' && item.status === 'Pending' && <>{['Accepted', 'Rejected'].map((status) => <AdminAction key={status} field="note" label={t(status === 'Accepted' ? 'admin:acceptAppeal' : 'admin:rejectAppeal')} target={item.username || item.userId} description={t('admin:reviewAppealDescription')} onConfirm={({ note }) => adminApi.reviewAppeal(item.id, { status, note })} />)}</>}
    {section === 'audit' && <HoldAction kind="Audit" id={item.id} held={item.legalHold} />}
  </div></article>
}
function AdminCase({ id }) {
  const { t } = useTranslation()
  const report = useQuery({ queryKey: ['workspace', 'admin', 'case', id], queryFn: ({ signal }) => adminApi.case(id, signal) })
  const download = useMutation({ mutationFn: async (evidenceId) => { const result = await adminApi.evidence(id, evidenceId); const url = URL.createObjectURL(result.blob); const anchor = document.createElement('a'); anchor.href = url; anchor.download = result.fileName; anchor.click(); setTimeout(() => URL.revokeObjectURL(url), 1000) } })
  if (report.isPending) return <StatePanel loading />
  if (report.error) return <StatePanel error={report.error} onRetry={report.refetch} />
  const { report: item, evidence } = report.data
  return <><section className="settings-card"><h2>{t('admin:caseTitle')}</h2><p className="page-description">{t('admin:caseDescription')}</p><span className="role-chip">{statusLabel(item.status)}</span><p className="case-reason">{item.reason}</p><dl className="audit-details"><div><dt>{t('admin:reporter')}</dt><dd>{item.reporterUserId}</dd></div><div><dt>{t('admin:target')}</dt><dd>{t(`reports:targets.${item.targetType}`)} · {item.targetId}</dd></div></dl>{item.status === 'Pending' && <div className="section-actions">{['Resolved', 'Dismissed'].map((status) => <AdminAction key={status} label={t(status === 'Resolved' ? 'admin:resolve' : 'admin:dismiss')} field="note" target={id} description={t('admin:resolveDescription')} onConfirm={({ note }) => adminApi.resolve(id, { status, note })} />)}</div>}</section><section className="settings-card"><h2>{t('admin:evidence')}</h2>{download.error && <p role="alert" className="field-error">{errorMessage(download.error)}</p>}{!evidence.length ? <StatePanel title={t('admin:noEvidence')} /> : evidence.map((entry) => <article className="evidence-card" key={entry.id}><time>{formatDate(entry.capturedAt)} · {formatTime(entry.capturedAt)}</time><p>{entry.evidenceContent || t('admin:evidenceMissing')}</p><div className="section-actions">{entry.storedFileId && <Button variant="outline" disabled={download.isPending} onClick={() => download.mutate(entry.id)}><Download size={16} />{t('admin:evidenceFile')}</Button>}<HoldAction kind="Evidence" id={entry.id} held={entry.legalHold} /></div></article>)}</section></>
}
function AdminRetention() {
  const { t } = useTranslation()
  const [kind, setKind] = useState('Evidence')
  const [requestedKind, setRequestedKind] = useState(null)
  const retention = useQuery({ queryKey: ['workspace', 'admin', 'retention'], queryFn: adminApi.retention })
  const files = useQuery({ queryKey: ['workspace', 'admin', 'fileRetention'], queryFn: adminApi.fileRetention })
  const plan = useInfiniteQuery({ queryKey: ['workspace', 'admin', 'retentionPlan', requestedKind], queryFn: ({ pageParam, signal }) => adminApi.plan(requestedKind, pageParam, signal), initialPageParam: 0, getNextPageParam: (page) => page.nextOffset ?? undefined, enabled: !!requestedKind })
  return <><section className="retention-intro"><ShieldCheck /><div><h2>{t('admin:retentionTitle')}</h2><p>{t('admin:retentionDescription')}</p></div></section>{retention.isPending ? <StatePanel loading /> : retention.error ? <StatePanel error={retention.error} onRetry={retention.refetch} /> : <MetricCards data={retention.data} namespace="admin:retentionMetrics" />}<section className="settings-card"><h2>{t('admin:planTitle')}</h2><p className="page-description">{t('admin:planDescription')}</p><form className="retention-plan-controls" onSubmit={(event) => { event.preventDefault(); if (kind === requestedKind) plan.refetch(); else setRequestedKind(kind) }}><label htmlFor="retention-kind">{t('admin:kind')}</label><select id="retention-kind" value={kind} onChange={(event) => setKind(event.target.value)}>{['Evidence', 'Audit'].map((value) => <option value={value} key={value}>{t(`admin:kinds.${value}`)}</option>)}</select><Button type="submit" disabled={plan.isFetching}>{t('admin:loadPlan')}</Button></form>{requestedKind && (plan.isPending ? <StatePanel loading /> : plan.error ? <StatePanel error={plan.error} onRetry={plan.refetch} /> : <>{!plan.data.pages.some((page) => page.items.length) ? <StatePanel title={t('admin:empty')} /> : plan.data.pages.flatMap((page) => page.items).map((item) => <article className="retention-plan-row" key={item.id}><div><strong>{item.id}</strong><p>{t('admin:eligibleSince', { date: formatDate(item.eligibleSince) })}</p></div>{requestedKind === 'Audit' && <HoldAction kind="Audit" id={item.id} held={false} />}</article>)}{plan.hasNextPage && <Button variant="outline" disabled={plan.isFetchingNextPage} onClick={() => plan.fetchNextPage()}>{t('admin:more')}</Button>}</>)}</section><section className="settings-card"><h2>{t('admin:filesTitle')}</h2>{files.isPending ? <StatePanel loading /> : files.error ? <StatePanel error={files.error} onRetry={files.refetch} /> : <><p className="page-description">{t(files.data.deletionEnabled ? 'admin:deleteEnabled' : 'admin:deleteDisabled')}</p>{!files.data.items.length ? <StatePanel title={t('admin:empty')} /> : files.data.items.map((item) => <article className="retention-plan-row" key={item.id}><div><strong>{item.id}</strong><p>{t(`admin:cleanupReasons.${item.reason}`, { defaultValue: t('admin:cleanupReason') })}</p></div><span>{formatNumber(item.sizeBytes / 1024, { maximumFractionDigits: 1 })} {t('files:kib')}</span></article>)}{files.data.hasMore && <p className="field-help">{t('admin:filesMore')}</p>}</>}</section></>
}
