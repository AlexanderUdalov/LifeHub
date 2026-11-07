import { describe, expect, it } from 'vitest'
import { appearanceColors, backgrounds, accents, contrast } from '../appearance'
import { normalizeMobileTabs, replaceMobileTab, mobilePageIds } from '../navigation'

describe('Appearance contrast', () => {
  it('keeps text, secondary text and controls readable on preset and arbitrary backgrounds', () => {
    const surfaces = [
      ...backgrounds.flatMap((x) => [x.light, x.dark]),
      '#000000',
      '#ffffff',
      '#777777',
      '#ff00ff',
      '#00ffff',
      '#ffff00',
    ]
    for (const bg of surfaces)
      for (const accent of accents.flatMap((x) => [x.light, x.dark])) {
        const c = appearanceColors(bg, accent)
        for (const surface of [c.bg, c.card, c.side, c.soft])
          for (const text of [c.text, c.muted, c.primary])
            expect(contrast(text, surface)).toBeGreaterThanOrEqual(4.5)
        expect(contrast(c.on, c.primary)).toBeGreaterThanOrEqual(4.5)
      }
  })
})
describe('Mobile navigation', () => {
  it('migrates missing or invalid preferences to three unique reachable sections', () => {
    expect(normalizeMobileTabs(null)).toEqual(['today', 'tasks', 'habits'])
    expect(normalizeMobileTabs(['journal', 'journal', 'deleted'])).toEqual([
      'journal',
      'today',
      'tasks',
    ])
  })
  it('swaps pinned sections and preserves the requested order without duplicates', () => {
    for (const key of mobilePageIds)
      for (const position of [0, 1, 2]) {
        const next = replaceMobileTab(['today', 'tasks', 'habits'], position, key)
        expect(next[position]).toBe(key)
        expect(new Set(next).size).toBe(3)
      }
    expect(replaceMobileTab(['today', 'tasks', 'journal'], 0, 'journal')).toEqual([
      'journal',
      'tasks',
      'today',
    ])
  })
})
