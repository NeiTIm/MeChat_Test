import { defineConfig } from '@playwright/test'
import { loadEnv } from 'vite'
const secure = loadEnv('e2e', process.cwd(), 'MECHAT_E2E_')
for (const key of ['MECHAT_E2E_IDENTIFIER', 'MECHAT_E2E_PASSWORD']) if (!process.env[key] && secure[key]) process.env[key] = secure[key]
export default defineConfig({
  testDir: './tests/e2e', outputDir: './test-results/live', testMatch: '**/*.live.spec.js', workers: 1,
  reporter: 'list',
  use: { baseURL: 'http://127.0.0.1:5173', trace: 'off', screenshot: 'off', video: 'off' },
  webServer: { command: 'npm.cmd run dev', url: 'http://127.0.0.1:5173', reuseExistingServer: true },
})
