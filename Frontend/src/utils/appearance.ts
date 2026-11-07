export type ThemeMode = 'auto' | 'light' | 'dark'
export type Design = 'focus' | 'rhythm'
export const accents = [
  { id: 'forest', light: '#375b43', dark: '#b3d3a2' },
  { id: 'ocean', light: '#315e86', dark: '#a6c9ef' },
  { id: 'iris', light: '#715489', dark: '#d0b4e5' },
  { id: 'clay', light: '#94583e', dark: '#e8b399' },
]
export const backgrounds = [
  { id: 'base', light: '#f7f8f5', dark: '#171d19' },
  { id: 'sand', light: '#f5efe4', dark: '#252018' },
  { id: 'mist', light: '#edf2f7', dark: '#19232d' },
  { id: 'lilac', light: '#f3edf5', dark: '#251e2b' },
]
export const isHex = (value: unknown): value is string =>
  typeof value === 'string' && /^#[\da-f]{6}$/i.test(value)
export function luminance(hex: string): number {
  const v = hex
    .slice(1)
    .match(/../g)!
    .map((n) => parseInt(n, 16) / 255)
    .map((n) => (n <= 0.04045 ? n / 12.92 : ((n + 0.055) / 1.055) ** 2.4))
  return 0.2126 * v[0]! + 0.7152 * v[1]! + 0.0722 * v[2]!
}
export function contrast(a: string, b: string): number {
  const x = luminance(a),
    y = luminance(b)
  return (Math.max(x, y) + 0.05) / (Math.min(x, y) + 0.05)
}
export function mix(a: string, b: string, amount: number): string {
  return (
    '#' +
    a
      .slice(1)
      .match(/../g)!
      .map((v, i) =>
        Math.round(
          parseInt(v, 16) * (1 - amount) + parseInt(b.slice(1 + i * 2, 3 + i * 2), 16) * amount,
        )
          .toString(16)
          .padStart(2, '0'),
      )
      .join('')
  )
}
export function appearanceColors(bg: string, accent: string, base = false) {
  const dark = luminance(bg) < 0.179
  let card = base ? (dark ? '#222b25' : '#ffffff') : mix(bg, '#ffffff', dark ? 0.055 : 0.78)
  let side = base ? (dark ? '#1c241f' : '#eff2ec') : mix(bg, dark ? '#ffffff' : '#000000', 0.025)
  const readable = (color: string) =>
    [bg, card, side].every((surface) => contrast(color, surface) >= 4.5)
  let text = dark ? '#e7eee8' : '#29382f'
  if (!readable(text))
    text = contrast('#000000', bg) >= contrast('#ffffff', bg) ? '#000000' : '#ffffff'
  const safeSurface = luminance(text) < 0.5 ? '#ffffff' : '#000000'
  for (let i = 0; i < 100 && contrast(text, card) < 4.5; i++) card = mix(card, safeSurface, 0.05)
  for (let i = 0; i < 100 && contrast(text, side) < 4.5; i++) side = mix(side, safeSurface, 0.05)
  const adjust = (color: string) => {
    for (let i = 0; i <= 100; i++) {
      const c = mix(color, text, i / 100)
      if (readable(c)) return c
    }
    return text
  }
  const muted = adjust(dark ? '#abb8ae' : '#617067')
  const primary = adjust(accent)
  const on = contrast('#ffffff', primary) >= contrast('#000000', primary) ? '#ffffff' : '#000000'
  let soft = card
  for (let amount = 0.1; amount >= 0; amount -= 0.01) {
    const candidate = mix(card, primary, amount)
    if ([text, muted, primary].every((color) => contrast(color, candidate) >= 4.5)) {
      soft = candidate
      break
    }
  }
  return {
    bg,
    card,
    side,
    text,
    muted,
    primary,
    on,
    soft,
    line: mix(card, text, 0.19),
    alert: adjust(dark ? '#f2ada1' : '#a94b40'),
    dark,
  }
}
