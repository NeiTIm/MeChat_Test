import { useLayoutEffect } from 'react'
import { useThemeStore } from '@/stores/theme-store'

export function ThemeProvider({ children }) {
  const theme = useThemeStore((state) => state.theme)
  useLayoutEffect(() => {
    document.documentElement.classList.toggle('dark', theme === 'dark')
    document.documentElement.style.colorScheme = theme
    document.querySelector('meta[name="theme-color"]')?.setAttribute('content', theme === 'dark' ? '#171621' : '#6355d8')
  }, [theme])
  return children
}
