import { useInfiniteQuery } from '@tanstack/react-query'

export function useConversationHistory({ area, id, historyBefore, cachedTarget, api }) {
  const history = useInfiniteQuery({
    queryKey: ['workspace', area, id, ...(historyBefore ? [historyBefore] : []), 'messages'],
    queryFn: ({ pageParam, signal }) => api.history(id, pageParam, signal),
    initialPageParam: historyBefore,
    refetchOnMount: !cachedTarget,
    getNextPageParam: (last) => (last.hasMore ? last.nextCursor : undefined),
    enabled: !!id,
  })
  return history
}
