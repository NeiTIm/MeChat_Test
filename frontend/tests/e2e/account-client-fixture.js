// Test-only HTTP simulation. This is not evidence of authenticated backend acceptance.
export async function accountFixture(context, page, handler) {
  await context.addCookies([{ name: 'webchat_csrf', value: 'client-fixture', url: 'http://127.0.0.1:5173' }])
  const me = { id: 'actor', username: 'minhanh', email: 'minhanh@example.test', displayName: 'Minh Anh', bio: '', systemRole: 'User' }
  await page.route('**/hubs/**', (route) => route.fulfill({ status: 503 }))
  await page.route('**/api/**', async (route) => {
    if (await handler?.(route, me)) return
    const path = new URL(route.request().url()).pathname
    if (path === '/api/auth/refresh') return route.fulfill({ json: { accessToken: 'test-only', expiresAt: '2099-01-01T00:00:00Z', userId: me.id, username: me.username } })
    if (path === '/api/users/me') return route.fulfill({ json: me })
    return route.fulfill({ status: 404 })
  })
}
