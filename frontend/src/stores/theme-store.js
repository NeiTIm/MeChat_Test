import { create } from 'zustand'

function initialTheme() {
  try {
    return localStorage.getItem('mechat-theme') === 'dark' ? 'dark' : 'light'
  } catch {
    return 'light'
  }
}
export const useThemeStore = create((set) => ({
  theme: initialTheme(),
  toggle: () =>
    set(({ theme }) => {
      const next = theme === 'light' ? 'dark' : 'light'
      try {
        localStorage.setItem('mechat-theme', next)
      } catch {
        /* Private browsing may deny storage. */
      }
      return { theme: next }
    }),
}))
