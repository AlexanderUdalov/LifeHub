import type { AddictionWithResetsDTO } from '@/api/AddictionsAPI'
import { fromDateOnlyString, getWeekKey, toDateOnlyString } from '@/utils/dateOnly'

export const TRIGGER_OUTCOME_OVERCAME = 0

export type WeekTrendBin = {
  weekKey: string
  label: string
  count: number
}

export type AddictionStatsResult = {
  resetCount: number
  maxStreakDays: number
  avgStreakDays: number
  cleanDaysPercentLast30: number
  daysInLast30Window: number
  weekTrend: WeekTrendBin[]
  resetByWeekdayMonFirst: number[]
  triggerTotal: number
  triggerSuccessPercent: number | null
}

function daysBetweenDateOnly(from: string, to: string): number {
  const a = fromDateOnlyString(from)
  const b = fromDateOnlyString(to)
  return Math.round((b.getTime() - a.getTime()) / 86_400_000)
}

function enumerateDateOnlyInclusive(from: string, to: string): string[] {
  if (from > to) return []
  const out: string[] = []
  const cur = fromDateOnlyString(from)
  const end = fromDateOnlyString(to)
  while (cur.getTime() <= end.getTime()) {
    out.push(toDateOnlyString(cur))
    cur.setDate(cur.getDate() + 1)
  }
  return out
}

function maxDateOnly(a: string, b: string): string {
  return a >= b ? a : b
}

function addDaysToDateOnly(yyyymmdd: string, deltaDays: number): string {
  const d = fromDateOnlyString(yyyymmdd)
  d.setDate(d.getDate() + deltaDays)
  return toDateOnlyString(d)
}

function uniqueSortedDates(dates: string[]): string[] {
  return [...new Set(dates)].sort()
}

function computeStreakSegments(createdDay: string, resetDaysSorted: string[], todayDay: string): number[] {
  if (resetDaysSorted.length === 0) {
    return [Math.max(0, daysBetweenDateOnly(createdDay, todayDay))]
  }
  const out: number[] = []
  out.push(Math.max(0, daysBetweenDateOnly(createdDay, resetDaysSorted[0]!) - 1))
  for (let i = 1; i < resetDaysSorted.length; i++) {
    const prev = resetDaysSorted[i - 1]!
    const cur = resetDaysSorted[i]!
    out.push(Math.max(0, daysBetweenDateOnly(prev, cur) - 1))
  }
  const last = resetDaysSorted[resetDaysSorted.length - 1]!
  out.push(Math.max(0, daysBetweenDateOnly(last, todayDay)))
  return out
}

function lastNWeekKeysDesc(n: number, ref: Date): string[] {
  const monday = fromDateOnlyString(getWeekKey(ref))
  const keys: string[] = []
  for (let i = 0; i < n; i++) {
    const d = new Date(monday)
    d.setDate(monday.getDate() - i * 7)
    keys.push(getWeekKey(d))
  }
  return keys.reverse()
}

function weekdayMonFirstFromDateOnly(dateStr: string): number {
  const d = fromDateOnlyString(dateStr)
  const js = d.getDay()
  return (js + 6) % 7
}

export function buildAddictionStats(
  addiction: AddictionWithResetsDTO,
  now: Date,
  weekTrendCount: number,
  intlLocale: string
): AddictionStatsResult {
  const todayDay = toDateOnlyString(now)
  const createdDay = toDateOnlyString(new Date(addiction.addiction.createdAt))

  const resetDaysFromPayload = addiction.resets.map((r) => r.date)
  const resetDaysUnique = uniqueSortedDates(resetDaysFromPayload)

  const segments = computeStreakSegments(createdDay, resetDaysUnique, todayDay)
  const maxStreakDays = segments.length ? Math.max(...segments) : 0
  const avgStreakDays =
    segments.length > 0 ? Math.round((segments.reduce((a, b) => a + b, 0) / segments.length) * 10) / 10 : 0

  const resetDaySet = new Set(resetDaysFromPayload)
  const windowEnd = todayDay
  const windowStartRaw = addDaysToDateOnly(windowEnd, -29)
  const periodStart = maxDateOnly(createdDay, windowStartRaw)
  const daysInLast30Window = enumerateDateOnlyInclusive(periodStart, windowEnd).length
  let cleanInWindow = 0
  for (const d of enumerateDateOnlyInclusive(periodStart, windowEnd)) {
    if (!resetDaySet.has(d)) cleanInWindow++
  }
  const cleanDaysPercentLast30 =
    daysInLast30Window > 0 ? Math.round((cleanInWindow / daysInLast30Window) * 1000) / 10 : 0

  const weekKeys = lastNWeekKeysDesc(weekTrendCount, now)
  const weekCounts = new Map<string, number>()
  for (const k of weekKeys) weekCounts.set(k, 0)
  for (const r of addiction.resets) {
    const wk = getWeekKey(fromDateOnlyString(r.date))
    if (weekCounts.has(wk)) weekCounts.set(wk, (weekCounts.get(wk) ?? 0) + 1)
  }
  const weekTrend: WeekTrendBin[] = weekKeys.map((weekKey) => {
    const monday = fromDateOnlyString(weekKey)
    const label = monday.toLocaleDateString(intlLocale, { day: 'numeric', month: 'short' })
    return { weekKey, label, count: weekCounts.get(weekKey) ?? 0 }
  })

  const resetByWeekdayMonFirst = [0, 0, 0, 0, 0, 0, 0]
  for (const r of addiction.resets) {
    const idx = weekdayMonFirstFromDateOnly(r.date)
    resetByWeekdayMonFirst[idx] = (resetByWeekdayMonFirst[idx] ?? 0) + 1
  }

  const triggerTotal = addiction.triggerEvents.length
  const overcame = addiction.triggerEvents.filter((e) => e.outcome === TRIGGER_OUTCOME_OVERCAME).length
  const triggerSuccessPercent =
    triggerTotal > 0 ? Math.round((overcame / triggerTotal) * 1000) / 10 : null

  return {
    resetCount: addiction.resets.length,
    maxStreakDays,
    avgStreakDays,
    cleanDaysPercentLast30,
    daysInLast30Window,
    weekTrend,
    resetByWeekdayMonFirst,
    triggerTotal,
    triggerSuccessPercent
  }
}
