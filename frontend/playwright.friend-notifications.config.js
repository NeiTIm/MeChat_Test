import { defineConfig } from '@playwright/test'
import bcr from './playwright.bcr.config'

// Isolated HTTP simulation, unreachable live proxy; flags affect only this test server.
export default defineConfig({
  ...bcr,
  testMatch: '**/friend-notifications.spec.js',
  outputDir: './test-results/friend-notifications',
  reporter: 'line',
})
