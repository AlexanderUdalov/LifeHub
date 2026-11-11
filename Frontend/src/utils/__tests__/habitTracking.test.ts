import { describe, expect, it } from 'vitest'
import type { HabitDTO, HabitWithHistoryDTO } from '@/api/HabitsAPI'
import {
  canMarkDay,
  dateRange,
  isHabitScheduled,
  pendingSchedule,
  planStats,
  scheduleOn,
  weekCompletions,
} from '../habitTracking'

const habit = (overrides: Partial<HabitDTO> = {}): HabitDTO => ({
  id: 'habit',
  title: 'Read',
  color: '#52b58b',
  recurrenceRule: 'FREQ=DAILY',
  timesPerWeekGoal: null,
  goalId: null,
  lifeAreaId: null,
  startDate: '2026-10-01',
  state: 'active',
  inactivePeriods: [],
  schedules: [
    { effectiveFrom: '2026-10-01', recurrenceRule: 'FREQ=DAILY', timesPerWeekGoal: null },
  ],
  ...overrides,
})
const item = (h = habit()): HabitWithHistoryDTO => ({ habit: h, currentStreak: 0, history: [] })

describe('habit tracking rules', () => {
  it('selects the historical schedule and exposes the pending change', () => {
    const h = habit({
      schedules: [
        { effectiveFrom: '2026-10-01', recurrenceRule: 'FREQ=DAILY', timesPerWeekGoal: null },
        {
          effectiveFrom: '2026-11-12',
          recurrenceRule: 'FREQ=WEEKLY;BYDAY=MO',
          timesPerWeekGoal: null,
        },
      ],
    })
    expect(scheduleOn(h, '2026-11-11').rule).toBe('FREQ=DAILY')
    expect(scheduleOn(h, '2026-11-12').rule).toBe('FREQ=WEEKLY;BYDAY=MO')
    expect(pendingSchedule(h, '2026-11-11')?.effectiveFrom).toBe('2026-11-12')
    expect(isHabitScheduled(h, '2026-11-13')).toBe(false)
    expect(isHabitScheduled(h, '2026-11-16')).toBe(true)
  })

  it('excludes days before tracking and pauses, with an exclusive resume date', () => {
    const h = habit({ inactivePeriods: [{ startDate: '2026-11-01', endDate: '2026-11-05' }] })
    expect(isHabitScheduled(h, '2026-09-30')).toBe(false)
    expect(isHabitScheduled(h, '2026-11-04')).toBe(false)
    expect(isHabitScheduled(h, '2026-11-05')).toBe(true)
  })

  it('keeps skips separate and excludes today and future days from past plan statistics', () => {
    const h = item()
    h.history = [
      { date: '2026-11-09', status: 'full' },
      { date: '2026-11-10', status: 'skip' },
      { date: '2026-11-11', status: 'full' },
    ]
    expect(planStats(h, '2026-11-09', '2026-11-15', '2026-11-11')).toEqual({
      done: 1,
      planned: 1,
      skipped: 1,
    })
  })

  it('does not put flexible weekly habits in the daily plan', () => {
    const h = item(
      habit({
        schedules: [
          { effectiveFrom: '2026-10-01', recurrenceRule: 'FREQ=WEEKLY', timesPerWeekGoal: 3 },
        ],
      }),
    )
    h.history = [
      { date: '2026-11-08', status: 'full' },
      { date: '2026-11-09', status: 'full' },
      { date: '2026-11-11', status: 'full' },
    ]
    expect(planStats(h, '2026-11-01', '2026-11-15', '2026-11-11').planned).toBe(0)
    expect(weekCompletions(h, '2026-11-11')).toBe(2)
    expect(canMarkDay(h, '2026-11-12', '2026-11-11')).toBe(false)
  })

  it('allows clearing a preserved mark on a now-inactive day', () => {
    const h = item(
      habit({ state: 'paused', inactivePeriods: [{ startDate: '2026-11-11', endDate: null }] }),
    )
    h.history = [{ date: '2026-11-11', status: 'full' }]
    expect(canMarkDay(h, '2026-11-11', '2026-11-11')).toBe(true)
    expect(isHabitScheduled(h.habit, '2026-11-11')).toBe(false)
  })

  it('enumerates local calendar days across a month boundary', () => {
    expect(dateRange('2026-10-30', '2026-11-02')).toEqual([
      '2026-10-30',
      '2026-10-31',
      '2026-11-01',
      '2026-11-02',
    ])
  })
})
