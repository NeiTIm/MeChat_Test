import { defineConfig } from '@playwright/test'
import regression from './playwright.g2-regression.config'

export default defineConfig({
  ...regression,
  testMatch: '**/message-notification-grouping.spec.js',
  outputDir: './test-results/message-notifications',
  reporter: 'line',
})
