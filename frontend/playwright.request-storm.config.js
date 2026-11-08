import { defineConfig } from '@playwright/test'

export default defineConfig({
  testDir: './tests/e2e', testMatch: '**/request-storm.spec.js', workers: 1, timeout: 100000,
  outputDir: './test-results/request-storm', reporter: 'list',
  use: { baseURL: 'http://127.0.0.1:5185', trace: 'off', viewport: { width: 1366, height: 900 } },
  webServer: {
    command: 'npm.cmd run dev -- --port 5185', url: 'http://127.0.0.1:5185', reuseExistingServer: false,
    env: { API_PROXY_TARGET: 'http://127.0.0.1:9', VITE_BCR_NOTIFICATION_LIFECYCLE_ENABLED: 'false', VITE_BCR_FRIEND_REQUEST_SEEN_ENABLED: 'false' },
  },
})
