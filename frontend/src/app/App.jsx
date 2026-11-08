import { QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider } from 'react-router'
import { queryClient } from '@/lib/query-client'
import { ThemeProvider } from './providers/ThemeProvider'
import { router } from './router'
import { ToastViewport } from '@/components/ToastViewport'

export function App() {
  return <QueryClientProvider client={queryClient}><ThemeProvider><RouterProvider router={router} /><ToastViewport /></ThemeProvider></QueryClientProvider>
}
