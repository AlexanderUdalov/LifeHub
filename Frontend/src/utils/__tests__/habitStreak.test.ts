import { describe, it, expect } from 'vitest'
import { calcTimesPerWeekStreak, calcFixedDaysStreak, type HabitCompletion } from '../habitStreak'
import { getWeekKey } from '../dateOnly'

/** Create a Date for a given YYYY-MM-DD string (local timezone). */
function d(s: string): Date {
  const [y, m, day] = s.split('-').map(Number)
  return new Date(y!, m! - 1, day!)
}

/** Build a completion lookup from a map of date-string → status. */
function completionFn(map: Record<string, HabitCompletion>): (day: Date) => HabitCompletion {
  return (day: Date) => {
    const key = `${day.getFullYear()}-${String(day.getMonth() + 1).padStart(2, '0')}-${String(day.getDate()).padStart(2, '0')}`
    return map[key] ?? 'none'
  }
}

/** Build fullCountByWeek map from a completion map. */
function buildWeekCounts(map: Record<string, HabitCompletion>): Map<string, number> {
  const result = new Map<string, number>()
  for (const [dateStr, status] of Object.entries(map)) {
    if (status !== 'full') continue
    const wk = getWeekKey(d(dateStr))
    result.set(wk, (result.get(wk) ?? 0) + 1)
  }
  return result
}

/** Generate an array of consecutive days (Mon-Sun-Mon-Sun…). */
function dayRange(start: string, count: number): Date[] {
  const s = d(start)
  const result: Date[] = []
  for (let i = 0; i < count; i++) {
    const dt = new Date(s)
    dt.setDate(s.getDate() + i)
    result.push(dt)
  }
  return result
}

// ─────────────────────────────────────────────
// calcTimesPerWeekStreak
// ─────────────────────────────────────────────
describe('calcTimesPerWeekStreak', () => {
  // 2026-02-23 is Monday
  const WEEK1_MON = '2026-02-23'
  const WEEK2_MON = '2026-03-02'

  it('returns 0 when the target day is not full', () => {
    const days = dayRange(WEEK1_MON, 7)
    const comp = completionFn({})
    const counts = buildWeekCounts({})

    expect(calcTimesPerWeekStreak(days, 0, 3, comp, counts)).toBe(0)
  })

  it('returns 1 when only the target day is full (single day)', () => {
    const days = dayRange(WEEK1_MON, 7)
    const comp = completionFn({ '2026-02-25': 'full' }) // Wed
    const counts = buildWeekCounts({ '2026-02-25': 'full' })

    expect(calcTimesPerWeekStreak(days, 2, 3, comp, counts)).toBe(1)
  })

  it('counts full days within the same week without requiring goal met', () => {
    const days = dayRange(WEEK1_MON, 7)
    const map: Record<string, HabitCompletion> = {
      '2026-02-23': 'full', // Mon
      '2026-02-25': 'full', // Wed
      '2026-02-27': 'full', // Fri
    }
    const comp = completionFn(map)
    const counts = buildWeekCounts(map)

    expect(calcTimesPerWeekStreak(days, 4, 3, comp, counts)).toBe(3) // Fri: streak = Mon+Wed+Fri
  })

  it('streak continues across weeks when previous week meets the goal', () => {
    const days = dayRange(WEEK1_MON, 14) // 2 weeks
    const map: Record<string, HabitCompletion> = {
      // Week 1: 3 full (meets goal=3)
      '2026-02-23': 'full', // Mon
      '2026-02-25': 'full', // Wed
      '2026-02-27': 'full', // Fri
      // Week 2: 2 full so far
      '2026-03-03': 'full', // Tue
      '2026-03-05': 'full', // Thu
    }
    const comp = completionFn(map)
    const counts = buildWeekCounts(map)

    // Thu of week 2 (index 10): streak should span both weeks = 5
    expect(calcTimesPerWeekStreak(days, 10, 3, comp, counts)).toBe(5)
  })

  it('streak breaks across weeks when previous week does NOT meet the goal', () => {
    const days = dayRange(WEEK1_MON, 14)
    const map: Record<string, HabitCompletion> = {
      // Week 1: only 2 full (goal=3 not met)
      '2026-02-23': 'full', // Mon
      '2026-02-25': 'full', // Wed
      // Week 2: 2 full
      '2026-03-03': 'full', // Tue
      '2026-03-05': 'full', // Thu
    }
    const comp = completionFn(map)
    const counts = buildWeekCounts(map)

    // Thu of week 2 (index 10): streak = 2 (only current week)
    expect(calcTimesPerWeekStreak(days, 10, 3, comp, counts)).toBe(2)
  })

  it('streak breaks when previous week has zero completions', () => {
    const days = dayRange(WEEK1_MON, 14)
    const map: Record<string, HabitCompletion> = {
      // Week 1: nothing
      // Week 2: 1 full
      '2026-03-04': 'full', // Wed
    }
    const comp = completionFn(map)
    const counts = buildWeekCounts(map)

    expect(calcTimesPerWeekStreak(days, 9, 1, comp, counts)).toBe(1)
  })

  it('goal=1: streak continues when previous week has at least 1 full', () => {
    const days = dayRange(WEEK1_MON, 14)
    const map: Record<string, HabitCompletion> = {
      '2026-02-26': 'full', // Thu week 1
      '2026-03-04': 'full', // Wed week 2
    }
    const comp = completionFn(map)
    const counts = buildWeekCounts(map)

    expect(calcTimesPerWeekStreak(days, 9, 1, comp, counts)).toBe(2)
  })

  it('handles non-full days between full days within the same week', () => {
    const days = dayRange(WEEK1_MON, 7)
    const map: Record<string, HabitCompletion> = {
      '2026-02-23': 'full', // Mon
      '2026-02-24': 'none', // Tue - gap
      '2026-02-25': 'full', // Wed
    }
    const comp = completionFn(map)
    const counts = buildWeekCounts(map)

    expect(calcTimesPerWeekStreak(days, 2, 2, comp, counts)).toBe(2)
  })

  it('non-full days near week boundary do not prevent boundary check', () => {
    // This is the core bug scenario: non-full days sit right around Monday,
    // so the old code would skip the week-boundary check.
    const days = dayRange(WEEK1_MON, 14)
    const map: Record<string, HabitCompletion> = {
      // Week 1: 2 full (goal=3 NOT met)
      '2026-02-23': 'full', // Mon
      '2026-02-24': 'full', // Tue
      // Sat, Sun are none; Mon of week 2 is none
      // Week 2:
      '2026-03-04': 'full', // Wed
    }
    const comp = completionFn(map)
    const counts = buildWeekCounts(map)

    // Wed of week 2: the old buggy code would count 3 (Mon+Tue+Wed),
    // because it never checked the boundary. The fix should return 1.
    expect(calcTimesPerWeekStreak(days, 9, 3, comp, counts)).toBe(1)
  })

  it('non-full days near week boundary allow continuation when goal IS met', () => {
    const days = dayRange(WEEK1_MON, 14)
    const map: Record<string, HabitCompletion> = {
      // Week 1: 3 full (goal=3 met)
      '2026-02-23': 'full', // Mon
      '2026-02-24': 'full', // Tue
      '2026-02-25': 'full', // Wed
      // Thu-Sun none, Mon of week 2 none
      // Week 2:
      '2026-03-04': 'full', // Wed
    }
    const comp = completionFn(map)
    const counts = buildWeekCounts(map)

    // Wed of week 2: streak spans both weeks = 4
    expect(calcTimesPerWeekStreak(days, 9, 3, comp, counts)).toBe(4)
  })

  it('previous week exceeds goal — streak still continues', () => {
    const days = dayRange(WEEK1_MON, 14)
    const map: Record<string, HabitCompletion> = {
      // Week 1: 5 full (exceeds goal=3)
      '2026-02-23': 'full',
      '2026-02-24': 'full',
      '2026-02-25': 'full',
      '2026-02-26': 'full',
      '2026-02-27': 'full',
      // Week 2:
      '2026-03-02': 'full', // Mon
    }
    const comp = completionFn(map)
    const counts = buildWeekCounts(map)

    expect(calcTimesPerWeekStreak(days, 7, 3, comp, counts)).toBe(6)
  })

  it('three weeks: streak spans all when every week meets goal', () => {
    const days = dayRange('2026-02-16', 21) // 3 weeks starting Mon Feb 16
    const map: Record<string, HabitCompletion> = {
      // Week 0 (Feb 16): 2 full, goal=2
      '2026-02-16': 'full',
      '2026-02-18': 'full',
      // Week 1 (Feb 23): 2 full
      '2026-02-23': 'full',
      '2026-02-26': 'full',
      // Week 2 (Mar 2): 1 full
      '2026-03-03': 'full',
    }
    const comp = completionFn(map)
    const counts = buildWeekCounts(map)

    // Tue of week 2 (index 15): all weeks meet goal=2
    expect(calcTimesPerWeekStreak(days, 15, 2, comp, counts)).toBe(5)
  })

  it('three weeks: middle week fails — streak only covers current week', () => {
    const days = dayRange('2026-02-16', 21)
    const map: Record<string, HabitCompletion> = {
      // Week 0: 2 full (goal=2)
      '2026-02-16': 'full',
      '2026-02-18': 'full',
      // Week 1: only 1 full (fails goal=2)
      '2026-02-23': 'full',
      // Week 2: 1 full
      '2026-03-03': 'full',
    }
    const comp = completionFn(map)
    const counts = buildWeekCounts(map)

    // Tue of week 2 (index 15): week 1 doesn't meet goal, so streak = 1
    expect(calcTimesPerWeekStreak(days, 15, 2, comp, counts)).toBe(1)
  })

  it('returns 0 for out-of-bounds index', () => {
    const days = dayRange(WEEK1_MON, 7)
    const comp = completionFn({})
    const counts = buildWeekCounts({})

    expect(calcTimesPerWeekStreak(days, -1, 3, comp, counts)).toBe(0)
    expect(calcTimesPerWeekStreak(days, 99, 3, comp, counts)).toBe(0)
  })

  it('skip-marked days are treated like none (not counted)', () => {
    const days = dayRange(WEEK1_MON, 7)
    const map: Record<string, HabitCompletion> = {
      '2026-02-23': 'full',
      '2026-02-24': 'skip',
      '2026-02-25': 'full',
    }
    const comp = completionFn(map)
    const counts = buildWeekCounts(map)

    // Skip is not full, so it's just ignored in the walk; count = 2
    expect(calcTimesPerWeekStreak(days, 2, 2, comp, counts)).toBe(2)
  })

  it('goal=7 (every day): streak breaks if any day is missing in previous week', () => {
    const days = dayRange(WEEK1_MON, 14)
    const map: Record<string, HabitCompletion> = {
      // Week 1: 6 of 7 (missing Sun)
      '2026-02-23': 'full',
      '2026-02-24': 'full',
      '2026-02-25': 'full',
      '2026-02-26': 'full',
      '2026-02-27': 'full',
      '2026-02-28': 'full',
      // '2026-03-01': missing (Sun)
      // Week 2:
      '2026-03-02': 'full', // Mon
    }
    const comp = completionFn(map)
    const counts = buildWeekCounts(map)

    // Mon of week 2: week 1 has 6 full but goal=7, streak = 1
    expect(calcTimesPerWeekStreak(days, 7, 7, comp, counts)).toBe(1)
  })

  it('goal=7 (every day): streak continues when all 7 days full', () => {
    const days = dayRange(WEEK1_MON, 14)
    const map: Record<string, HabitCompletion> = {
      '2026-02-23': 'full',
      '2026-02-24': 'full',
      '2026-02-25': 'full',
      '2026-02-26': 'full',
      '2026-02-27': 'full',
      '2026-02-28': 'full',
      '2026-03-01': 'full', // Sun
      '2026-03-02': 'full', // Mon week 2
    }
    const comp = completionFn(map)
    const counts = buildWeekCounts(map)

    expect(calcTimesPerWeekStreak(days, 7, 7, comp, counts)).toBe(8)
  })
})

// ─────────────────────────────────────────────
// calcFixedDaysStreak
// ─────────────────────────────────────────────
describe('calcFixedDaysStreak', () => {
  const WEEK_MON = '2026-02-23'

  it('returns 0 when target day is none', () => {
    const days = dayRange(WEEK_MON, 7)
    const comp = completionFn({})
    const disabled = days.map(() => false)

    expect(calcFixedDaysStreak(days, 2, comp, disabled)).toBe(0)
  })

  it('counts consecutive full days', () => {
    const days = dayRange(WEEK_MON, 7)
    const map: Record<string, HabitCompletion> = {
      '2026-02-23': 'full',
      '2026-02-24': 'full',
      '2026-02-25': 'full',
    }
    const comp = completionFn(map)
    const disabled = days.map(() => false)

    expect(calcFixedDaysStreak(days, 2, comp, disabled)).toBe(3)
  })

  it('skip-marked days are passed through, not breaking the streak', () => {
    const days = dayRange(WEEK_MON, 5)
    const map: Record<string, HabitCompletion> = {
      '2026-02-23': 'full',
      '2026-02-24': 'skip',
      '2026-02-25': 'full',
    }
    const comp = completionFn(map)
    const disabled = days.map(() => false)

    expect(calcFixedDaysStreak(days, 2, comp, disabled)).toBe(2)
  })

  it('disabled (off-schedule) days are skipped, not breaking the streak', () => {
    const days = dayRange(WEEK_MON, 5)
    const map: Record<string, HabitCompletion> = {
      '2026-02-23': 'full', // Mon
      // Tue disabled (off-schedule)
      '2026-02-25': 'full', // Wed
    }
    const comp = completionFn(map)
    // Tue is disabled
    const disabled = [false, true, false, true, true]

    expect(calcFixedDaysStreak(days, 2, comp, disabled)).toBe(2)
  })

  it('none day breaks the streak', () => {
    const days = dayRange(WEEK_MON, 5)
    const map: Record<string, HabitCompletion> = {
      '2026-02-23': 'full',
      '2026-02-24': 'none', // breaks
      '2026-02-25': 'full',
    }
    const comp = completionFn(map)
    const disabled = days.map(() => false)

    expect(calcFixedDaysStreak(days, 2, comp, disabled)).toBe(1)
  })
})
