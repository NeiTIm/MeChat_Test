import { expect, it } from 'vitest'
import { notificationMessageTarget } from '@/features/notifications/message-target'

it.each([['Message', '/chat'], ['GroupMessage', '/groups'], ['MessageRequest', '/message-requests']])('resolves %s through authorized message metadata', (type, base) => {
  expect(notificationMessageTarget({ type, referenceId: 'target' }, { id: 'target', conversationId: 'room', sequence: 12 })).toBe(`${base}/room?at=12&message=target`)
})
it.each([null, { id: 'other', conversationId: 'room', sequence: 1 }, { id: 'target', sequence: 1 }, { id: 'target', conversationId: 'room', sequence: 0 }, { id: 'target', conversationId: 'room', sequence: Number.MAX_SAFE_INTEGER }])('rejects mismatched or unusable message metadata', message => {
  expect(() => notificationMessageTarget({ type: 'Message', referenceId: 'target' }, message)).toThrow()
})
