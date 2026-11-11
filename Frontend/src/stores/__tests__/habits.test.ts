import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { habitsApi, type HabitWithHistoryDTO } from '@/api/HabitsAPI'
import { useHabitsStore } from '../habits'

vi.mock('@/api/HabitsAPI', () => ({
  habitsApi: {
    getHabits: vi.fn(),
    getHistory: vi.fn(),
    createHabit: vi.fn(),
    updateHabit: vi.fn(),
    setState: vi.fn(),
    setDayStatus: vi.fn(),
    deleteHabit: vi.fn(),
  },
}))
function fixture(): HabitWithHistoryDTO {
  return {
    habit: {
      id: 'habit',
      title: 'Read',
      color: '#52b58b',
      recurrenceRule: 'FREQ=DAILY',
      timesPerWeekGoal: null,
      goalId: null,
      lifeAreaId: null,
      startDate: '2026-01-01',
      state: 'active',
      inactivePeriods: [],
      schedules: [
        { effectiveFrom: '2026-01-01', recurrenceRule: 'FREQ=DAILY', timesPerWeekGoal: null },
      ],
    },
    history: [{ date: '2026-11-10', status: 'full' }],
    currentStreak: 100,
  }
}
beforeEach(() => {
  vi.resetAllMocks()
  setActivePinia(createPinia())
  vi.useFakeTimers()
  vi.setSystemTime(new Date(2026, 10, 11, 12))
  vi.mocked(habitsApi.getHabits).mockResolvedValue([fixture()])
})
afterEach(() => vi.useRealTimers())

describe('habit data and mutations', () => {
  it('uses the server streak without shortening it to the loaded history', async () => {
    const store = useHabitsStore()
    await store.fetchHabits()
    vi.mocked(habitsApi.setDayStatus).mockResolvedValue({
      date: '2026-11-11',
      status: 'full',
      currentStreak: 101,
    })
    await store.setDayStatus('habit', new Date(2026, 10, 11), 'full')
    expect(store.habits[0]?.currentStreak).toBe(101)
    expect(habitsApi.getHabits).toHaveBeenCalledTimes(1)
  })

  it('rolls back a failed mark and unlocks the habit', async () => {
    const store = useHabitsStore()
    await store.fetchHabits()
    vi.mocked(habitsApi.setDayStatus).mockRejectedValueOnce(new Error('offline'))
    await expect(store.setDayStatus('habit', new Date(2026, 10, 11), 'full')).rejects.toThrow(
      'offline',
    )
    expect(store.habits[0]?.history).toEqual(fixture().history)
    expect(store.isBusy('habit')).toBe(false)
  })

  it('blocks a second in-flight mark and ignores a stale refresh', async () => {
    const store = useHabitsStore()
    await store.fetchHabits()
    let resolveRefresh!: (v: HabitWithHistoryDTO[]) => void
    vi.mocked(habitsApi.getHabits).mockReturnValueOnce(
      new Promise((r) => {
        resolveRefresh = r
      }),
    )
    const refresh = store.fetchHabits()
    let resolveMark!: (v: { date: string; status: string; currentStreak: number }) => void
    vi.mocked(habitsApi.setDayStatus).mockReturnValueOnce(
      new Promise((r) => {
        resolveMark = r
      }),
    )
    const marking = store.setDayStatus('habit', new Date(2026, 10, 11), 'full')
    await store.setDayStatus('habit', new Date(2026, 10, 11), 'none')
    resolveRefresh([fixture()])
    await refresh
    const committed = fixture()
    committed.history.push({ date: '2026-11-11', status: 'full' })
    committed.currentStreak = 101
    vi.mocked(habitsApi.getHabits).mockResolvedValue([committed])
    resolveMark({ date: '2026-11-11', status: 'full', currentStreak: 101 })
    await marking
    expect(habitsApi.setDayStatus).toHaveBeenCalledTimes(1)
    expect(
      store.habits[0]?.history.some((d) => d.date === '2026-11-11' && d.status === 'full'),
    ).toBe(true)
  })

  it('preserves older loaded history after refreshing the recent window', async () => {
    const store = useHabitsStore()
    await store.fetchHabits()
    vi.mocked(habitsApi.getHistory).mockResolvedValueOnce({
      from: '2026-07-23',
      to: '2026-09-16',
      hasEarlier: true,
      items: [{ date: '2026-08-01', status: 'full' }],
    })
    await store.loadEarlierHistory('habit')
    await store.fetchHabits()
    expect(store.habits[0]?.history.map((d) => d.date)).toEqual(['2026-08-01', '2026-11-10'])
    expect(store.historyFrom.habit).toBe('2026-07-23')
  })

  it('changes lifecycle status while preserving every loaded mark', async () => {
    const store = useHabitsStore()
    await store.fetchHabits()
    const archived = {
      ...fixture().habit,
      state: 'archived',
      inactivePeriods: [{ startDate: '2026-11-11', endDate: null }],
    }
    vi.mocked(habitsApi.setState).mockResolvedValue({ habit: archived, currentStreak: 100 })
    await store.setState('habit', 'archived')
    expect(store.habits[0]?.habit.state).toBe('archived')
    expect(store.habits[0]?.history).toEqual(fixture().history)
  })
})
