import { useState } from 'react';
import { NavLink, useNavigate, useParams } from 'react-router';
import { useInfiniteQuery, useMutation, useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Search, UserPlus, MessageSquare, UsersRound } from 'lucide-react';
import { ApplicationLayout } from '@/components/layout/ApplicationLayout';
import { Avatar } from '@/components/Avatar';
import { StatePanel } from '@/components/StatePanel';
import { ConfirmAction } from '@/components/ConfirmAction';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { usersApi } from '@/features/users/api';
import { conversationsApi } from '@/features/conversations/api';
import { useAuthStore } from '@/stores/auth-store';
import { queryClient } from '@/lib/query-client';
import { errorMessage } from '@/lib/errors';
import { formatDate } from '@/i18n/format';
import { friendsApi } from './api';
import { ReportDialog } from '@/features/moderation/ReportsPage';
export function FriendsPage() {
  const {
    t
  } = useTranslation();
  const {
    section = 'list'
  } = useParams();
  const navigate = useNavigate();
  const [notice, setNotice] = useState('');
  const friends = useQuery({
    queryKey: ['workspace', 'friends'],
    queryFn: friendsApi.list
  });
  const requests = useQuery({
    queryKey: ['workspace', 'friendRequests'],
    queryFn: friendsApi.requests
  });
  const blocks = useQuery({
    queryKey: ['workspace', 'blocks'],
    queryFn: friendsApi.blocks
  });
  const action = useMutation({
    mutationFn: async ({
      method,
      id,
      success
    }) => {
      setNotice('');
      const result = await friendsApi[method](id);
      setNotice(t(method === 'send' && result?.status === 'Accepted' ? 'friends:accepted' : `friends:${success}`));
      // Blocking changes visibility; discard protected content before fetching fresh data.
      if (method === 'block') await queryClient.resetQueries({
        queryKey: ['workspace']
      });else await queryClient.invalidateQueries({
        queryKey: ['workspace']
      });
    }
  });
  const openChat = useMutation({
    mutationFn: conversationsApi.open,
    onSuccess: conversation => {
      queryClient.invalidateQueries({
        queryKey: ['workspace']
      });
      navigate(`/chat/${conversation.id}`);
    }
  });
  const run = (method, id, success) => action.mutateAsync({
    method,
    id,
    success
  });
  const shared = {
    friends: friends.data || [],
    requests: requests.data,
    blocks: blocks.data || [],
    run,
    pending: action.isPending || openChat.isPending,
    onMessage: id => openChat.mutate(id)
  };
  const current = section === 'list' ? friends : section === 'blocked' ? blocks : requests;
  const rows = section === 'incoming' ? requests.data?.incoming : section === 'outgoing' ? requests.data?.outgoing : section === 'blocked' ? blocks.data : friends.data;
  return <ApplicationLayout area="friends" title={t('friends:title')} description={t('friends:description')}><div className="settings-layout"><nav className="settings-tabs" aria-label={t('friends:title')}>{['list', 'incoming', 'outgoing', 'search', 'blocked'].map(tab => <NavLink key={tab} to={`/friends/${tab}`} className={section === tab ? 'active' : ''}>{t(`friends:tabs.${tab}`)}{['incoming', 'outgoing'].includes(tab) && !!requests.data?.[tab]?.length && <span className="tab-count">{requests.data[tab].length}</span>}</NavLink>)}</nav><section className="settings-content">
    {notice && <p className="form-feedback feedback-success" role="status">{notice}</p>}
    {(action.error || openChat.error) && <p className="form-feedback feedback-error" role="alert">{errorMessage(action.error || openChat.error)}</p>}
    {section === 'search' ? <UserSearch {...shared} dependenciesError={friends.error || requests.error || blocks.error} dependenciesPending={friends.isPending || requests.isPending || blocks.isPending} onRetryDependencies={() => {
          friends.refetch();
          requests.refetch();
          blocks.refetch();
        }} /> : !['list', 'incoming', 'outgoing', 'blocked'].includes(section) ? <StatePanel title={t('common:nothing-here-just-yet')} /> : current.isPending ? <StatePanel loading /> : current.error ? <StatePanel error={current.error} onRetry={current.refetch} /> : !rows?.length ? <div className="people-empty"><UsersRound /><StatePanel title={t(`friends:${section === 'incoming' ? 'noIncoming' : section === 'outgoing' ? 'noOutgoing' : section === 'blocked' ? 'noBlocked' : 'empty'}`)} description={t('friends:emptyDescription')} /></div> : <div className="people-list">{rows.map(row => <FriendRow key={row.id || row.userId} item={row} section={section} {...shared} />)}</div>}
  </section></div></ApplicationLayout>;
}
function PublicProfile({
  id,
  fallback
}) {
  const {
    t
  } = useTranslation();
  const [open, setOpen] = useState(false);
  const profile = useQuery({
    queryKey: ['workspace', 'profile', id],
    queryFn: ({
      signal
    }) => usersApi.profile(id, signal),
    enabled: open
  });
  return <Dialog open={open} onOpenChange={setOpen}><DialogTrigger asChild><button type="button" className="person-identity"><Avatar fileId={fallback.avatarFileId} name={fallback.displayName || fallback.username || '?'} /><span><strong>{fallback.displayName || fallback.username || t('friends:profileTitle')}</strong>{fallback.username && <small>@{fallback.username}</small>}</span></button></DialogTrigger><DialogContent className="details-dialog"><DialogHeader><DialogTitle>{t('friends:profileTitle')}</DialogTitle><DialogDescription>{t('friends:profileDescription')}</DialogDescription></DialogHeader>{profile.isPending ? <StatePanel loading /> : profile.error ? <StatePanel error={profile.error} onRetry={profile.refetch} /> : <div className="public-profile"><Avatar fileId={profile.data.avatarFileId} name={profile.data.displayName} /><h2>{profile.data.displayName}</h2><p>@{profile.data.username}</p><p>{profile.data.bio || t('friends:noBio')}</p><ReportDialog targetType="User" targetId={id} /></div>}</DialogContent></Dialog>;
}
function FriendRow({
  item,
  section,
  run,
  pending,
  onMessage
}) {
  const {
    t
  } = useTranslation();
  const isRequest = section === 'incoming' || section === 'outgoing';
  const userId = isRequest ? section === 'incoming' ? item.senderUserId : item.receiverUserId : item.userId;
  const profile = useQuery({
    queryKey: ['workspace', 'profile', userId],
    queryFn: ({
      signal
    }) => usersApi.profile(userId, signal),
    enabled: isRequest
  });
  const display = isRequest ? profile.data || {} : item;
  const perform = (method, success) => run(method, isRequest ? item.id : userId, success).catch(() => {});
  return <article className="person-row"><div className="person-info"><PublicProfile id={userId} fallback={display} /><p className="person-date">{t(`friends:${isRequest ? 'requestDate' : section === 'blocked' ? 'blockedSince' : 'friendSince'}`, {
          date: formatDate(item.createdAt || item.blockedAt || item.since)
        })}</p>{profile.error && <p className="field-help">{t('friends:profileUnavailable')}</p>}</div><div className="person-actions">
    {section === 'incoming' ? <><Button disabled={pending} onClick={() => perform('accept', 'accepted')}>{t('friends:accept')}</Button><Button variant="outline" disabled={pending} onClick={() => perform('reject', 'rejected')}>{t('friends:reject')}</Button></> : section === 'outgoing' ? <Button variant="outline" disabled={pending} onClick={() => perform('cancel', 'cancelled')}>{t('friends:cancel')}</Button> : section === 'blocked' ? <ConfirmAction disabled={pending} label={t('friends:unblock')} description={t('friends:unblockConfirmation')} onConfirm={() => run('unblock', userId, 'unblocked')} /> : <><Button disabled={pending} onClick={() => onMessage(userId)}><MessageSquare size={16} />{t('friends:message')}</Button><ConfirmAction disabled={pending} label={t('friends:unfriend')} description={t('friends:unfriendConfirmation')} onConfirm={() => run('unfriend', userId, 'unfriended')} /></>}
    {section !== 'blocked' && <ConfirmAction disabled={pending} destructive label={t('friends:block')} description={t('friends:blockConfirmation')} onConfirm={() => run('block', userId, 'blocked')} />}
  </div></article>;
}
function UserSearch({
  friends,
  requests,
  blocks,
  run,
  pending,
  onMessage,
  dependenciesError,
  dependenciesPending,
  onRetryDependencies
}) {
  const {
    t
  } = useTranslation();
  const ownId = useAuthStore(state => state.access?.userId);
  const [input, setInput] = useState('');
  const [query, setQuery] = useState('');
  const results = useInfiniteQuery({
    queryKey: ['workspace', 'userSearch', query],
    queryFn: ({
      pageParam,
      signal
    }) => usersApi.search(query, pageParam, signal),
    initialPageParam: undefined,
    getNextPageParam: page => page.hasMore ? page.nextCursor : undefined,
    enabled: !!query
  });
  const items = [...new Map((results.data?.pages || []).flatMap(page => page.items).map(item => [item.id, item])).values()];
  return <><form className="people-search" onSubmit={event => {
      event.preventDefault();
      setQuery(input.trim());
    }}><label htmlFor="people-query">{t('friends:searchLabel')}</label><div><Input id="people-query" value={input} onChange={event => setInput(event.target.value)} placeholder={t('friends:searchPlaceholder')} maxLength={100} /><Button type="submit" disabled={!input.trim()}><Search size={18} />{t('friends:search')}</Button></div></form>
    {dependenciesPending ? <StatePanel loading /> : dependenciesError ? <StatePanel error={dependenciesError} onRetry={onRetryDependencies} /> : !query ? <StatePanel title={t('friends:searchHint')} /> : results.isPending ? <StatePanel loading /> : results.error ? <StatePanel error={results.error} onRetry={results.refetch} /> : <><p className="field-help">{t('friends:resultCount', {
          count: items.length
        })}</p>{!items.length ? <StatePanel title={t('friends:noResults')} description={t('friends:noResultsDescription')} /> : <div className="people-list">{items.map(person => {
          const friend = friends.some(row => row.userId === person.id);
          const blocked = blocks.some(row => row.userId === person.id);
          const outgoing = requests?.outgoing?.some(row => row.receiverUserId === person.id);
          const incoming = requests?.incoming?.find(row => row.senderUserId === person.id);
          return <article className="person-row" key={person.id}><PublicProfile id={person.id} fallback={person} /><div className="person-actions">{person.id === ownId ? <span className="role-chip">{t('friends:yourAccount')}</span> : blocked ? <ConfirmAction disabled={pending} label={t('friends:unblock')} description={t('friends:unblockConfirmation')} onConfirm={() => run('unblock', person.id, 'unblocked')} /> : friend ? <><span className="role-chip">{t('friends:alreadyFriends')}</span><Button disabled={pending} onClick={() => onMessage(person.id)}><MessageSquare size={16} />{t('friends:message')}</Button></> : outgoing ? <span className="role-chip">{t('friends:pending')}</span> : <Button disabled={pending} onClick={() => run(incoming ? 'accept' : 'send', incoming?.id || person.id, incoming ? 'accepted' : 'sent').catch(() => {})}><UserPlus size={16} />{t(incoming ? 'friends:accept' : 'friends:send')}</Button>}</div></article>;
        })}</div>}{results.hasNextPage && <Button className="load-more-people" variant="outline" disabled={results.isFetchingNextPage} onClick={() => results.fetchNextPage()}>{results.isFetchingNextPage ? t('common:saving') : t('friends:more')}</Button>}</>}
  </>;
}
