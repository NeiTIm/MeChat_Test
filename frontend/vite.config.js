import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { fileURLToPath, URL } from 'node:url'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const target = env.API_PROXY_TARGET || 'http://localhost:5080'
  return {
    plugins: [react(), tailwindcss()],
    resolve: { alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) } },
    server: {
      port: 5173,
      strictPort: true,
      proxy: {
        '/api': { target, changeOrigin: true },
        '/hubs': { target, changeOrigin: true, ws: true },
      },
    },
    build: { sourcemap: false, rollupOptions: { output: { manualChunks(id) {
      const path = id.replaceAll('\\', '/')
      if (!path.includes('/node_modules/')) return
      if (['/react/', '/react-dom/', '/react-router/', '/scheduler/'].some((name) => path.includes(name))) return 'vendor-react'
      if (['/i18next/', '/react-i18next/', '/i18next-browser-languagedetector/'].some((name) => path.includes(name))) return 'vendor-i18n'
      if (path.includes('/@tanstack/')) return 'vendor-query'
    } } } },
    test: {
      environment: 'jsdom',
      setupFiles: ['./tests/setup.js'],
      include: ['tests/**/*.test.{js,jsx}'],
      restoreMocks: true,
    },
  }
})
