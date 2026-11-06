import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AxiosError } from 'axios';
import { adoptSession, clearSession, configureSessionLoss, csrfToken, http, refreshSession } from '@/lib/http';
import { useAuthStore } from '@/stores/auth-store';
const access = {
  accessToken: 'test-access-only',
  userId: 'test-user',
  username: 'test',
  expiresAt: '2099-01-01T00:00:00Z'
};
const originalAdapter = http.defaults.adapter;
const response = (config, data, status = 200) => ({
  config,
  data,
  status,
  statusText: '',
  headers: {}
});
function unauthorized(config) {
  return Promise.reject(new AxiosError('unauthorized', 'ERR_BAD_REQUEST', config, null, response(config, {}, 401)));
}
beforeEach(() => {
  configureSessionLoss(vi.fn());
  clearSession();
  document.cookie = 'webchat_csrf=test-csrf; path=/';
});
afterEach(() => {
  http.defaults.adapter = originalAdapter;
  document.cookie = 'webchat_csrf=; Max-Age=0; path=/';
});
describe('refresh-cookie session boundary', () => {
  it('rejects malformed login and refresh responses without authenticating', async () => {
    expect(() => adoptSession({ userId: 'wrong-shape' })).toThrow('INVALID_SESSION_RESPONSE');
    expect(useAuthStore.getState().status).toBe('anonymous');
    http.defaults.adapter = config => Promise.resolve(response(config, null, 204));
    await expect(refreshSession()).rejects.toThrow('INVALID_SESSION_RESPONSE');
    expect(useAuthStore.getState().status).toBe('anonymous');
  });
  it('reads the rotated CSRF token when retrying logout after refresh', async () => {
    adoptSession({
      ...access,
      accessToken: 'expired'
    });
    let logoutCalls = 0;
    http.defaults.adapter = config => {
      if (config.url === '/auth/refresh') {
        document.cookie = 'webchat_csrf=rotated-csrf; path=/';
        return Promise.resolve(response(config, access));
      }
      logoutCalls += 1;
      if (logoutCalls === 1) return unauthorized(config);
      expect(config.headers['X-CSRF-Token']).toBe('rotated-csrf');
      return Promise.resolve(response(config, null, 204));
    };
    await http.post('/auth/logout', null, {
      headers: {
        'X-CSRF-Token': 'test-csrf'
      }
    });
    expect(logoutCalls).toBe(2);
  });
  it('never replays an old account operation with a different refreshed identity', async () => {
    adoptSession(access);
    const urls = [];
    http.defaults.adapter = config => {
      urls.push(config.url);
      return config.url === '/auth/refresh' ? Promise.resolve(response(config, {
        ...access,
        userId: 'different-user'
      })) : unauthorized(config);
    };
    await expect(http.patch('/users/me', {
      displayName: 'test-only'
    })).rejects.toBeTruthy();
    expect(urls).toEqual(['/users/me', '/auth/refresh']);
    expect(useAuthStore.getState().status).toBe('anonymous');
  });
  it('single-flights simultaneous refreshes and reads the current CSRF cookie', async () => {
    let calls = 0;
    http.defaults.adapter = async config => {
      calls += 1;
      expect(config.headers['X-CSRF-Token']).toBe('test-csrf');
      await Promise.resolve();
      return response(config, access);
    };
    await Promise.all([refreshSession(), refreshSession(), refreshSession()]);
    expect(calls).toBe(1);
    expect(useAuthStore.getState().access).toEqual(access);
    document.cookie = 'webchat_csrf=rotated; path=/';
    expect(csrfToken()).toBe('rotated');
  });
  it('does not send an anonymous refresh without a CSRF cookie', async () => {
    document.cookie = 'webchat_csrf=; Max-Age=0; path=/';
    const adapter = vi.fn();
    http.defaults.adapter = adapter;
    expect(await refreshSession()).toBeNull();
    expect(adapter).not.toHaveBeenCalled();
  });
  it('never refreshes a rejected public login', async () => {
    const requests = [];
    http.defaults.adapter = config => {
      requests.push(config.url);
      return unauthorized(config);
    };
    await expect(http.post('/auth/login', {}, {
      skipAuth: true
    })).rejects.toBeTruthy();
    expect(requests).toEqual(['/auth/login']);
  });
  it('recovers a protected 401 exactly once', async () => {
    adoptSession({
      ...access,
      accessToken: 'expired'
    });
    const requests = [];
    http.defaults.adapter = config => {
      requests.push(config.url);
      if (config.url === '/auth/refresh') return Promise.resolve(response(config, access));
      return config.headers.Authorization === `Bearer ${access.accessToken}` ? Promise.resolve(response(config, {
        ok: true
      })) : unauthorized(config);
    };
    expect((await http.get('/users/me')).data).toEqual({
      ok: true
    });
    expect(requests).toEqual(['/users/me', '/auth/refresh', '/users/me']);
  });
  it('clears identity after rejected refresh and does not recurse', async () => {
    adoptSession(access);
    let calls = 0;
    http.defaults.adapter = config => {
      calls += 1;
      return unauthorized(config);
    };
    await expect(http.get('/users/me')).rejects.toBeTruthy();
    expect(calls).toBe(2);
    expect(useAuthStore.getState().status).toBe('anonymous');
  });
  it('does not resurrect a session when logout races refresh', async () => {
    let finish;
    http.defaults.adapter = config => new Promise(resolve => {
      finish = () => resolve(response(config, access));
    });
    const pending = refreshSession();
    await vi.waitFor(() => expect(finish).toBeTypeOf('function'));
    clearSession();
    finish();
    expect(await pending).toBeNull();
    expect(useAuthStore.getState().access).toBeNull();
  });
});
