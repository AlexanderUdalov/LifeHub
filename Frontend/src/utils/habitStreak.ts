import { getWeekKey } from './dateOnly'

export type HabitCompletion = 'none' | 'skip' | 'full'

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
