import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { nextTick } from 'vue'
import { usePreferencesStore } from '../preferences'

describe('Preference persistence', () => {
  const values = new Map<string, string>()
  beforeEach(() => {
    values.clear()
    vi.stubGlobal('localStorage', {
      getItem: (key: string) => values.get(key) ?? null,
      setItem: (key: string, value: string) => values.set(key, value),
    })
    vi.stubGlobal('window', { matchMedia: () => ({ matches: false, addEventListener: vi.fn() }) })
    vi.stubGlobal('document', {
      documentElement: {
        classList: { toggle: vi.fn() },
        dataset: {},
        style: { setProperty: vi.fn() },
      },
    })
    setActivePinia(createPinia())
  })
  afterEach(() => vi.unstubAllGlobals())
  it('restores exact light and dark backgrounds independently with the chosen tab order', async () => {
    const store = usePreferencesStore()
    store.theme = 'light'
    store.setCustomBackground('#f3e7d1')
    store.theme = 'dark'
    store.setCustomBackground('#15212b')
    store.accent = 'clay'
    store.design = 'rhythm'
    store.setTab(0, 'journal')
    store.setTab(2, 'addictions')
    await nextTick()
    setActivePinia(createPinia())
    const restored = usePreferencesStore()
    expect(restored.backgroundColor).toBe('#15212b')
    restored.theme = 'light'
    expect(restored.backgroundColor).toBe('#f3e7d1')
    expect(restored.accent).toBe('clay')
    expect(restored.design).toBe('rhythm')
    expect(restored.mobileTabs).toEqual(['journal', 'tasks', 'addictions'])
    await nextTick()
  })
  it('ignores broken saved JSON instead of blocking the app', () => {
    values.set('lifehub-appearance-v1', '{broken')
    expect(usePreferencesStore().mobileTabs).toEqual(['today', 'tasks', 'habits'])
  })
})
