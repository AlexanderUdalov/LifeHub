import { defineStore } from 'pinia'
import { computed, ref, watch } from 'vue'
import {
  accents,
  backgrounds,
  appearanceColors,
  isHex,
  type ThemeMode,
  type Design,
} from '@/utils/appearance'
import { normalizeMobileTabs, replaceMobileTab, type MobilePageId } from '@/utils/navigation'

const key = 'lifehub-appearance-v1'
const read = (name: string) => {
  try {
    return localStorage.getItem(name)
  } catch {
    return null
  }
}
const load = (): Record<string, unknown> => {
  try {
    return JSON.parse(read(key) ?? '{}') ?? {}
  } catch {
    return {}
  }
}

export const usePreferencesStore = defineStore('preferences', () => {
  const saved = load()
  const theme = ref<ThemeMode>(
    ['auto', 'light', 'dark'].includes(String(saved.theme ?? read('theme')))
      ? ((saved.theme ?? read('theme')) as ThemeMode)
      : 'auto',
  )
  const design = ref<Design>(saved.design === 'rhythm' ? 'rhythm' : 'focus')
  const accent = ref(
    accents.some((x) => x.id === saved.accent) || saved.accent === 'custom'
      ? String(saved.accent)
      : isHex(read('theme-primary-color'))
        ? 'custom'
        : 'ocean',
  )
  const customAccent = ref(
    isHex(saved.customAccent) ? saved.customAccent : (read('theme-primary-color') ?? '#315e86'),
  )
  const background = ref(
    backgrounds.some((x) => x.id === saved.background) || saved.background === 'custom'
      ? String(saved.background)
      : 'base',
  )
  const backgroundLight = ref(isHex(saved.backgroundLight) ? saved.backgroundLight : '#f7f8f5')
  const backgroundDark = ref(isHex(saved.backgroundDark) ? saved.backgroundDark : '#171d19')
  const mobileTabs = ref(normalizeMobileTabs(saved.mobileTabs))
  const media = window.matchMedia('(prefers-color-scheme: dark)')
  const systemDark = ref(media.matches)
  media.addEventListener('change', (e) => {
    systemDark.value = e.matches
  })
  const mode = computed(() =>
    theme.value === 'auto' ? (systemDark.value ? 'dark' : 'light') : theme.value,
  )
  const backgroundColor = computed(() =>
    background.value === 'custom'
      ? mode.value === 'dark'
        ? backgroundDark.value
        : backgroundLight.value
      : (backgrounds.find((x) => x.id === background.value) ?? backgrounds[0]!)[mode.value],
  )
  const colors = computed(() =>
    appearanceColors(
      backgroundColor.value,
      accent.value === 'custom' && isHex(customAccent.value)
        ? customAccent.value
        : (accents.find((x) => x.id === accent.value) ?? accents[1]!)[mode.value],
      background.value === 'base',
    ),
  )
  function setCustomBackground(value: string) {
    if (!isHex(value)) return
    background.value = 'custom'
    if (mode.value === 'dark') backgroundDark.value = value
    else backgroundLight.value = value
  }
  function setTab(position: number, value: MobilePageId) {
    mobileTabs.value = replaceMobileTab(mobileTabs.value, position, value)
  }
  function apply() {
    const c = colors.value,
      root = document.documentElement
    root.classList.toggle('p-dark', mode.value === 'dark')
    root.dataset.design = design.value
    root.style.colorScheme = c.dark ? 'dark' : 'light'
    const vars: Record<string, string> = {
      'app-bg': c.bg,
      'app-card': c.card,
      'app-side': c.side,
      'app-text': c.text,
      'app-muted': c.muted,
      'app-line': c.line,
      'app-accent': c.primary,
      'app-on': c.on,
      'app-soft': c.soft,
      'app-alert': c.alert,
      'p-content-background': c.card,
      'p-content-hover-background': c.soft,
      'p-content-border-color': c.line,
      'p-content-color': c.text,
      'p-text-color': c.text,
      'p-text-muted-color': c.muted,
      'p-primary-color': c.primary,
      'p-primary-hover-color': c.primary,
      'p-primary-active-color': c.primary,
      'p-primary-contrast-color': c.on,
      'p-highlight-background': c.soft,
      'p-highlight-color': c.primary,
      'p-card-background': c.card,
      'p-card-color': c.text,
      'p-card-shadow': 'none',
      'p-form-field-background': c.card,
      'p-form-field-color': c.text,
      'p-form-field-border-color': c.line,
      'p-form-field-placeholder-color': c.muted,
      'p-overlay-modal-background': c.card,
      'p-overlay-modal-color': c.text,
      'p-overlay-popover-background': c.card,
      'p-overlay-select-background': c.card,
      'p-surface-0': c.card,
      'p-surface-50': c.bg,
      'p-surface-100': c.side,
      'p-surface-200': c.line,
      'ds-radius-lg': design.value === 'focus' ? '12px' : '20px',
      'ds-radius-md': design.value === 'focus' ? '12px' : '20px',
    }
    for (const [name, value] of Object.entries(vars)) root.style.setProperty('--' + name, value)
  }
  watch([colors, mode, design], apply, { immediate: true })
  watch(
    [theme, design, accent, customAccent, background, backgroundLight, backgroundDark, mobileTabs],
    () => {
      try {
        localStorage.setItem(
          key,
          JSON.stringify({
            theme: theme.value,
            design: design.value,
            accent: accent.value,
            customAccent: customAccent.value,
            background: background.value,
            backgroundLight: backgroundLight.value,
            backgroundDark: backgroundDark.value,
            mobileTabs: mobileTabs.value,
          }),
        )
      } catch {
        /* Appearance remains usable without storage. */
      }
    },
    { deep: true },
  )
  return {
    theme,
    design,
    accent,
    customAccent,
    background,
    backgroundLight,
    backgroundDark,
    mobileTabs,
    mode,
    colors,
    backgroundColor,
    setCustomBackground,
    setTab,
  }
})
