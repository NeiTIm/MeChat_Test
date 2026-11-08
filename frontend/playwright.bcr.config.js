import { defineConfig } from '@playwright/test'

// Isolated simulation-only frontend. Never enables flags on the user's running app.
export default defineConfig({
  testDir: './tests/e2e', testMatch: '**/bcr-client.spec.js', workers: 1,
  outputDir: './test-results/bcr',
  reporter: [['list'], ['json', { outputFile: 'test-results/bcr-results.json' }]],
  use: { baseURL: 'http://127.0.0.1:5183', browserName: 'chromium', trace: 'off' },
  webServer: {
    command: 'npm.cmd run dev -- --port 5183', url: 'http://127.0.0.1:5183', reuseExistingServer: false,
    env: { VITE_BCR_NOTIFICATION_LIFECYCLE_ENABLED: 'true', VITE_BCR_FRIEND_REQUEST_SEEN_ENABLED: 'true', API_PROXY_TARGET: 'http://127.0.0.1:9' },
  },
})
