import { Moon, Sun } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { useThemeStore } from '@/stores/theme-store'
import { useTranslation } from 'react-i18next'

export function ThemeToggle() {
  const { theme, toggle } = useThemeStore()
  const { t } = useTranslation()
  return <Button variant="ghost" size="icon" className="icon-button" onClick={toggle}
    aria-label={t(`common:${theme === 'light' ? 'darkMode' : 'lightMode'}`)} title={t(`common:${theme === 'light' ? 'darkMode' : 'lightMode'}`)}>
    {theme === 'light' ? <Moon /> : <Sun />}
  </Button>
}
