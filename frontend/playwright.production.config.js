import { defineConfig } from '@playwright/test'
export default defineConfig({
  testDir: './tests/e2e', outputDir: './test-results/production', testMatch: '**/*.production.spec.js', workers: 1,
  reporter: 'list',
  use: { baseURL: 'http://127.0.0.1:4173', trace: 'off' },
  webServer: { command: 'npm.cmd run preview -- --port 4173', url: 'http://127.0.0.1:4173', reuseExistingServer: false },
})
