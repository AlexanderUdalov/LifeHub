export const mobilePageIds = [
  'today',
  'tasks',
  'habits',
  'goals',
  'journal',
  'addictions',
  'lifeareas',
] as const
export type MobilePageId = (typeof mobilePageIds)[number]
export const defaultMobileTabs: MobilePageId[] = ['today', 'tasks', 'habits']

export function normalizeMobileTabs(value: unknown): MobilePageId[] {
  const selected = Array.isArray(value)
    ? [...new Set(value.filter((key): key is MobilePageId => mobilePageIds.includes(key)))]
    : []
  return [...selected, ...defaultMobileTabs.filter((key) => !selected.includes(key))].slice(0, 3)
}

export function replaceMobileTab(
  tabs: MobilePageId[],
  position: number,
  key: MobilePageId,
): MobilePageId[] {
  const next = normalizeMobileTabs(tabs)
  if (position < 0 || position > 2 || !mobilePageIds.includes(key)) return next
  const other = next.indexOf(key)
  if (other >= 0) next[other] = next[position]!
  next[position] = key
  return next
}
