import { defineConfig } from '@playwright/test'
import baseline from './playwright.config'

// Run existing client regression with no route to the real API and no server reuse.
export default defineConfig({
  ...baseline,
  outputDir: './test-results/g2-regression',
  reporter: [['list'], ['json', { outputFile: 'test-results/g2-regression-results.json' }]],
  use: { ...baseline.use, baseURL: 'http://127.0.0.1:5184' },
  webServer: {
    command: 'npm.cmd run dev -- --port 5184', url: 'http://127.0.0.1:5184', reuseExistingServer: false,
    env: { API_PROXY_TARGET: 'http://127.0.0.1:9', VITE_BCR_NOTIFICATION_LIFECYCLE_ENABLED: 'false', VITE_BCR_FRIEND_REQUEST_SEEN_ENABLED: 'false' },
  },
})
