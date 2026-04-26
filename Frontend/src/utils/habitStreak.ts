import { fromDateOnlyString, getWeekKey, getWeekStart, startOfDay, toDateOnlyString } from './dateOnly'

export type HabitCompletion = 'none' | 'skip' | 'full'

export interface WeekSummary {
  weekKey: string
  monday: Date
  count: number
  goal: number
  goalMet: boolean
}

/**
 * Build per-week completion summary for *past* weeks only (excludes current week).
 * Returns weeks from oldest to newest (index 0 = 8w ago, last = 1w ago), for "8w ago … last week" strip.
 * goal must be 1–7.
 */
export function getWeekSummaries(
  history: Array<{ date: string; status?: string | null }>,
  goal: number,
  numberOfWeeks = 8,
): WeekSummary[] {
  if (goal < 1 || goal > 7) return []

  const fullCountByWeek = new Map<string, number>()
  for (const h of history) {
    if (h.status?.toLowerCase() !== 'full') continue
    const weekKey = getWeekKey(fromDateOnlyString(h.date))
    fullCountByWeek.set(weekKey, (fullCountByWeek.get(weekKey) ?? 0) + 1)
  }

  const today = startOfDay(new Date())
  const summaries: WeekSummary[] = []

  for (let i = numberOfWeeks; i >= 1; i--) {
    const d = new Date(today)
    d.setDate(today.getDate() - 7 * i)
    const weekKey = getWeekKey(d)
    const monday = getWeekStart(d)
    const count = fullCountByWeek.get(weekKey) ?? 0
    const goalMet = count >= goal
    summaries.push({ weekKey, monday, count, goal, goalMet })
  }

  return summaries
}

/**
 * Current streak in weeks for "N times per week" habits.
 * Counts consecutive completed weeks where each has >= goal full completions.
 * If the current week has not met the goal yet, falls back to previous week
 * (similar to day-based streak fallback when today is unmarked).
 * goal must be 1–7.
 */
export function getCurrentWeeksStreak(
  history: Array<{ date: string; status?: string | null }>,
  goal: number,
  todayDate: Date = new Date(),
): number {
  if (goal < 1 || goal > 7) return 0

  const fullCountByWeek = new Map<string, number>()
  for (const h of history) {
    if (h.status?.toLowerCase() !== 'full') continue
    const weekKey = getWeekKey(fromDateOnlyString(h.date))
    fullCountByWeek.set(weekKey, (fullCountByWeek.get(weekKey) ?? 0) + 1)
  }

  const today = startOfDay(todayDate)
  const currentWeekCount = fullCountByWeek.get(getWeekKey(today)) ?? 0
  const startOffset = currentWeekCount >= goal ? 0 : 1

  let streak = 0
  for (let i = startOffset; i < startOffset + 52; i++) {
    const d = new Date(today)
    d.setDate(today.getDate() - 7 * i)
    const weekKey = getWeekKey(d)
    const count = fullCountByWeek.get(weekKey) ?? 0
    if (count >= goal) streak++
    else break
  }

  return streak
}

/**
 * Fallback streak for day-based habits when backend currentStreak is 0,
 * but user hasn't marked today yet.
 *
 * Counts consecutive `full` days backwards from today (if full) or yesterday.
 * `skip` days are ignored and do not break the chain.
 */
export function getCurrentDayBasedStreakFallback(
  history: Array<{ date: string; status?: string | null }>,
  todayDate: Date = new Date(),
): number {
  const byDate = new Map<string, HabitCompletion>()
  for (const h of history) {
    const raw = (h.status ?? 'none').toLowerCase()
    const normalized: HabitCompletion = raw === 'full' || raw === 'skip' || raw === 'none' ? raw : 'none'
    byDate.set(h.date, normalized)
  }

  const today = startOfDay(todayDate)
  const todayStatus = byDate.get(toDateOnlyString(today)) ?? 'none'
  const cursor = new Date(today)
  if (todayStatus !== 'full') {
    cursor.setDate(cursor.getDate() - 1)
  }

  let streak = 0
  for (let i = 0; i < 365; i++) {
    const key = toDateOnlyString(cursor)
    const status = byDate.get(key) ?? 'none'

    if (status === 'full') {
      streak++
    } else if (status === 'none') {
      break
    }

    cursor.setDate(cursor.getDate() - 1)
  }

  return streak
}

/**
 * Streak for "N times per week" habits (no fixed weekdays).
 *
 * Counts consecutive full-completion days walking backwards from `index`.
 * When crossing a week boundary the *previous* (older) week must have
 * at least `goal` full completions, otherwise the streak breaks.
 * The current week is not checked against the goal because it may still
 * be in progress.
 */
export function calcTimesPerWeekStreak(
  days: Date[],
  index: number,
  goal: number,
  getCompletion: (day: Date) => HabitCompletion,
  fullCountByWeek: ReadonlyMap<string, number>,
): number {
  if (index < 0 || index >= days.length) return 0

  if (getCompletion(days[index]!) !== 'full') return 0

  let count = 1
  let currentWeekKey = getWeekKey(days[index]!)

  for (let j = index - 1; j >= 0; j--) {
    const dayWeekKey = getWeekKey(days[j]!)

    if (dayWeekKey !== currentWeekKey) {
      if ((fullCountByWeek.get(dayWeekKey) ?? 0) < goal) break
      currentWeekKey = dayWeekKey
    }

    if (getCompletion(days[j]!) !== 'full') continue
    count++
  }

  return count
}

/**
 * Streak for fixed-weekday habits (BYDAY rule).
 *
 * Counts consecutive full days backwards, skipping disabled (off-schedule)
 * days and skip-marked days. Stops on `none`.
 */
export function calcFixedDaysStreak(
  days: Date[],
  index: number,
  getCompletion: (day: Date) => HabitCompletion,
  disabledByIndex: boolean[],
): number {
  let count = 0
  for (let i = index; i >= 0; i--) {
    if (disabledByIndex[i]) continue
    const c = getCompletion(days[i]!)
    if (c === 'full') {
      count++
      continue
    }
    if (c === 'skip') continue
    break
  }
  return count
}
