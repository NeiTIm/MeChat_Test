import { test, expect } from '@playwright/test';
test('real backend health through the frontend proxy', async ({
  request
}) => {
  const result = await request.get('/api/health');
  expect(result.status()).toBe(200);
  expect(await result.json()).toEqual({
    status: 'ok',
    service: 'MeChat.Api'
  });
});
test('protected HTTP and hub negotiation reject an anonymous browser', async ({
  request
}) => {
  for (const route of ['/api/users/me', '/api/conversations', '/api/groups', '/api/admin/dashboard']) expect((await request.get(route)).status()).toBe(401);
  expect((await request.post('/hubs/chat/negotiate?negotiateVersion=1')).status()).toBe(401);
});
test('real auth rejects missing refresh CSRF and invalid login DTO without account attempts', async ({
  request
}) => {
  const refresh = await request.post('/api/auth/refresh');
  expect(refresh.status()).toBe(401);
  expect(await refresh.json()).toEqual({
    code: 'INVALID_REFRESH'
  });
  const login = await request.post('/api/auth/login', {
    data: {}
  });
  expect(login.status()).toBe(400);
  expect((await login.json()).errors).toHaveProperty('Identifier');
});
test('real public auth validates DTOs before business mutations', async ({ request }) => {
  for (const path of ['register', 'verify-email', 'resend-verification', 'forgot-password', 'reset-password', 'reactivate']) {
    const response = await request.post(`/api/auth/${path}`, { data: {} });
    expect(response.status(), path).toBe(400);
    expect(await response.json()).toHaveProperty('errors');
  }
});
