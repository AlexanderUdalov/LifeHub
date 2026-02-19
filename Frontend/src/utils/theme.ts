export type ThemeMode = 'light' | 'dark' | 'auto'

const THEME_STORAGE_KEY = 'theme'

export function applyTheme(mode: ThemeMode): void {
  if (mode === 'auto') {
    const isDark = window.matchMedia('(prefers-color-scheme: dark)').matches
    document.documentElement.classList.toggle('p-dark', isDark)
  } else {
    document.documentElement.classList.toggle('p-dark', mode === 'dark')
  }
}

export function getStoredTheme(): ThemeMode | null {
  const raw = localStorage.getItem(THEME_STORAGE_KEY)
  if (raw === 'light' || raw === 'dark' || raw === 'auto') return raw
  return null
}

export function setStoredTheme(mode: ThemeMode): void {
  localStorage.setItem(THEME_STORAGE_KEY, mode)
}
