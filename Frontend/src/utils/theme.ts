import { palette, updatePrimaryPalette, updateSurfacePalette, usePreset } from '@primeuix/themes'
import Aura from '@primeuix/themes/aura'

export type ThemeMode = 'light' | 'dark' | 'auto'

const THEME_STORAGE_KEY = 'theme'
const PRIMARY_COLOR_STORAGE_KEY = 'theme-primary-color'
const SURFACE_COLOR_STORAGE_KEY = 'theme-surface-color'

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

export function getStoredPrimaryColor(): string | null {
  const raw = localStorage.getItem(PRIMARY_COLOR_STORAGE_KEY)
  return raw && /^#[0-9A-Fa-f]{6}$/.test(raw) ? raw : null
}

export function setStoredPrimaryColor(hex: string | null): void {
  if (hex) localStorage.setItem(PRIMARY_COLOR_STORAGE_KEY, hex)
  else localStorage.removeItem(PRIMARY_COLOR_STORAGE_KEY)
}

export function getStoredSurfaceColor(): string | null {
  const raw = localStorage.getItem(SURFACE_COLOR_STORAGE_KEY)
  return raw && /^#[0-9A-Fa-f]{6}$/.test(raw) ? raw : null
}

export function setStoredSurfaceColor(hex: string | null): void {
  if (hex) localStorage.setItem(SURFACE_COLOR_STORAGE_KEY, hex)
  else localStorage.removeItem(SURFACE_COLOR_STORAGE_KEY)
}

function toPaletteToken(scale: Record<string, string>): Record<string, string> {
  const keys = [50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950]
  const out: Record<string, string> = {}
  for (const k of keys) {
    const v = scale[String(k)]
    if (v) out[k] = v
  }
  if (scale['0']) out[0] = scale['0']
  return out
}

export function applyPrimaryColor(hex: string | null): void {
  if (hex) {
    const scale = palette(hex)
    const token = typeof scale === 'object' && scale !== null ? toPaletteToken(scale as Record<string, string>) : undefined
    if (token) updatePrimaryPalette(token)
    setStoredPrimaryColor(hex)
  } else {
    setStoredPrimaryColor(null)
    usePreset(Aura)
    applyTheme(getStoredTheme() ?? 'auto')
    const surface = getStoredSurfaceColor()
    if (surface) applySurfaceColor(surface)
  }
}

export function applySurfaceColor(hex: string | null): void {
  if (hex) {
    const scale = palette(hex)
    const token = typeof scale === 'object' && scale !== null ? toPaletteToken(scale as Record<string, string>) : undefined
    if (token) updateSurfacePalette(token)
    setStoredSurfaceColor(hex)
  } else {
    setStoredSurfaceColor(null)
    usePreset(Aura)
    applyTheme(getStoredTheme() ?? 'auto')
    const primary = getStoredPrimaryColor()
    if (primary) applyPrimaryColor(primary)
  }
}

export function applyStoredCustomColors(): void {
  const primary = getStoredPrimaryColor()
  if (primary) applyPrimaryColor(primary)
  const surface = getStoredSurfaceColor()
  if (surface) applySurfaceColor(surface)
}
