import { describe, expect, it } from 'vitest';
import { errorMessage } from '@/lib/errors';
import { mergeMessages } from '@/features/conversations/WorkspacePage';
describe('heterogeneous API errors', () => {
  it.each([400, 401, 403, 404, 409, 429, 500])('normalizes %s with empty or non-JSON bodies', status => {
    expect(errorMessage({
      response: {
        status
      }
    })).toBeTruthy();
    expect(errorMessage({
      response: {
        status,
        data: '<html>private stack</html>'
      }
    })).not.toContain('private stack');
  });
  it('distinguishes credentials, timeout and network errors', () => {
    expect(errorMessage({
      response: {
        status: 401,
        data: {
          code: 'INVALID_CREDENTIALS_OR_ACCOUNT_STATE'
        }
      }
    })).toContain("thông tin đăng nhập");
    expect(errorMessage({
      code: 'ECONNABORTED'
    })).toContain("quá thời gian chờ");
    expect(errorMessage(new Error())).toContain("kết nối đến MeChat");
  });
});
it('deduplicates cursor overlap and renders messages in sequence order', () => {
  const pages = [{
    items: [{
      id: 'b',
      sequence: 2,
      content: 'edited'
    }, {
      id: 'c',
      sequence: 3
    }]
  }, {
    items: [{
      id: 'a',
      sequence: 1
    }, {
      id: 'b',
      sequence: 2,
      content: 'old'
    }]
  }];
  expect(mergeMessages(pages).map(message => message.id)).toEqual(['a', 'b', 'c']);
  expect(mergeMessages(pages)[1].content).toBe('edited');
});
