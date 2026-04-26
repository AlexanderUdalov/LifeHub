/**
 * Colors extracted from favicon.svg (logo).
 * Used for life area color selection to keep visual consistency.
 */
export const LOGO_COLORS = [
  '#c0ca33', // lime
  '#8bc34a',  // green
  '#00acc1', // cyan
  '#64b5f6', // light blue
  '#ba68c8', // purple
  '#f48fb1', // pink
  '#ff5252', // red
  '#ffb74d' // orange
] as const

/** Normalize hex color for comparison (lowercase, with #). */
export function normalizeHex(color: string): string {
  const s = color.trim()
  const hex = s.startsWith('#') ? s.slice(1) : s
  return '#' + hex.toLowerCase()
}

export function isLogoColor(color: string): boolean {
  const n = normalizeHex(color)
  return (LOGO_COLORS as readonly string[]).includes(n)
}

/** Index of color in LOGO_COLORS (for ordering). Non-logo colors return LOGO_COLORS.length. */
export function getLogoColorIndex(color: string): number {
  const n = normalizeHex(color)
  const i = (LOGO_COLORS as readonly string[]).indexOf(n)
  return i === -1 ? LOGO_COLORS.length : i
}
