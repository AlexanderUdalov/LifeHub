import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import {
  habitsApi,
  type HabitUpsertRequest,
  type HabitWithHistoryDTO,
  type HabitDayDTO,
  type HabitState,
  type HabitCompletion,
} from '@/api/HabitsAPI'
import { toDateOnlyString } from '@/utils/dateOnly'

export const useHabitsStore = defineStore('habits', () => {
  const habits = ref<HabitWithHistoryDTO[]>([])
  const isLoading = ref(false)
  const rangeDays = ref(56)
  const busy = ref(new Set<string>())
  const loadingHistory = ref(new Set<string>())
  const historyFrom = ref<Record<string, string>>({})
  let revision = 0
  let requestId = 0
  let refreshNeeded = false
  const habitsSorted = computed(() =>
    [...habits.value].sort((a, b) => a.habit.title.localeCompare(b.habit.title)),
  )
  const isBusy = (id: string) => busy.value.has(id)
  const find = (id: string) => habits.value.find((h) => h.habit.id === id)
  const mergeHistory = (older: HabitDayDTO[], newer: HabitDayDTO[]) =>
    [...new Map([...older, ...newer].map((d) => [d.date, d])).values()].sort((a, b) =>
      a.date.localeCompare(b.date),
    )

  async function fetchHabits(days = rangeDays.value): Promise<void> {
    if (busy.value.size) {
      refreshNeeded = true
      return
    }
    rangeDays.value = Math.max(56, days, rangeDays.value)
    const request = ++requestId,
      version = revision
    isLoading.value = true
    try {
      const data = await habitsApi.getHabits(rangeDays.value)
      if (request !== requestId) return
      if (version !== revision || busy.value.size) {
        if (!busy.value.size) return await fetchHabits(rangeDays.value)
        refreshNeeded = true
        return
      }
      const start = new Date()
      start.setDate(start.getDate() + 1 - rangeDays.value)
      const from = toDateOnlyString(start)
      habits.value = data.map((h) => {
        const old = find(h.habit.id)
        const preserved = old?.history.filter((d) => d.date < from) ?? []
        const loaded = from < h.habit.startDate ? h.habit.startDate : from
        historyFrom.value[h.habit.id] =
          historyFrom.value[h.habit.id] && historyFrom.value[h.habit.id]! < loaded
            ? historyFrom.value[h.habit.id]!
            : loaded
        return { ...h, history: mergeHistory(preserved, h.history) }
      })
    } finally {
      if (request === requestId) isLoading.value = false
    }
  }

  function finishMutation(id: string) {
    busy.value.delete(id)
    revision++
    if (!busy.value.size && refreshNeeded) {
      refreshNeeded = false
      void fetchHabits().catch(() => {
        refreshNeeded = true
      })
    }
  }

  function writeDay(h: HabitWithHistoryDTO, date: string, status: HabitCompletion) {
    h.history = h.history.filter((d) => d.date !== date)
    if (status !== 'none') h.history = mergeHistory(h.history, [{ date, status }])
  }

  async function setDayStatus(id: string, date: Date, status: HabitCompletion) {
    const h = find(id)
    if (!h || isBusy(id)) return
    busy.value.add(id)
    revision++
    const key = toDateOnlyString(date)
    const old = (h.history.find((d) => d.date === key)?.status as HabitCompletion) ?? 'none'
    writeDay(h, key, status)
    try {
      const result = await habitsApi.setDayStatus(id, date, status)
      h.currentStreak = result.currentStreak
    } catch (error) {
      writeDay(h, key, old)
      throw error
    } finally {
      finishMutation(id)
    }
  }

  async function createHabit(request: HabitUpsertRequest) {
    const created = await habitsApi.createHabit(request)
    revision++
    habits.value.push({ habit: created, history: [], currentStreak: 0 })
    historyFrom.value[created.id] = created.startDate
  }

  async function updateHabit(id: string, request: HabitUpsertRequest) {
    const h = find(id)
    if (!h || isBusy(id)) return
    busy.value.add(id)
    revision++
    try {
      h.habit = await habitsApi.updateHabit(id, request)
    } finally {
      finishMutation(id)
    }
  }

  async function setState(id: string, state: HabitState) {
    const h = find(id)
    if (!h || isBusy(id)) return
    busy.value.add(id)
    revision++
    try {
      const updated = await habitsApi.setState(id, state)
      h.habit = updated.habit
      h.currentStreak = updated.currentStreak
    } finally {
      finishMutation(id)
    }
  }

  async function loadEarlierHistory(id: string) {
    const h = find(id)
    if (!h || loadingHistory.value.has(id)) return
    loadingHistory.value.add(id)
    try {
      const page = await habitsApi.getHistory(id, historyFrom.value[id])
      // A recent mark wins if a user edits while the older page is in flight.
      const current = find(id)
      if (!current) return
      current.history = mergeHistory(page.items, current.history)
      historyFrom.value[id] = page.from
    } finally {
      loadingHistory.value.delete(id)
    }
  }

  async function ensureHistoryFrom(id: string, from: string) {
    while (
      find(id) &&
      historyFrom.value[id] &&
      historyFrom.value[id]! > from &&
      historyFrom.value[id]! > find(id)!.habit.startDate
    ) {
      const before = historyFrom.value[id]
      await loadEarlierHistory(id)
      if (historyFrom.value[id] === before) break
    }
  }

  async function deleteHabit(id: string) {
    if (isBusy(id)) return
    busy.value.add(id)
    revision++
    try {
      await habitsApi.deleteHabit(id)
      habits.value = habits.value.filter((h) => h.habit.id !== id)
    } finally {
      finishMutation(id)
    }
  }

  return {
    habits,
    habitsSorted,
    isLoading,
    rangeDays,
    busy,
    isBusy,
    loadingHistory,
    historyFrom,
    fetchHabits,
    setDayStatus,
    createHabit,
    updateHabit,
    setState,
    deleteHabit,
    loadEarlierHistory,
    ensureHistoryFrom,
  }
})
