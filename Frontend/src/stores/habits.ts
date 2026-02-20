import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { habitsApi, type HabitUpsertRequest, type HabitWithHistoryDTO } from '@/api/HabitsAPI'
import { toDateOnlyString } from '@/utils/dateOnly'

type HabitCompletion = 'none' | 'skip' | 'full'

export const useHabitsStore = defineStore('habits', () => {
  const habits = ref<HabitWithHistoryDTO[]>([])
  const isLoading = ref(false)
  const rangeDays = ref(14)

  async function fetchHabits(days = rangeDays.value) {
    rangeDays.value = days
    isLoading.value = true
    try {
      habits.value = await habitsApi.getHabits(days)
    } finally {
      isLoading.value = false
    }
  }

  const habitsSorted = computed(() => [...habits.value].sort((a, b) => a.habit.title.localeCompare(b.habit.title)))

  async function setDayStatus(habitId: string, date: Date, status: HabitCompletion) {
    const h = habits.value.find(x => x.habit.id === habitId)
    if (!h) return

    const dayKey = toDateOnlyString(date)
    const prev = [...h.history]

    const idx = h.history.findIndex(x => x.date === dayKey)
    if (status === 'none') {
      if (idx !== -1) h.history.splice(idx, 1)
    } else {
      const entry = { date: dayKey, status }
      if (idx === -1) h.history.push(entry)
      else h.history[idx] = entry
    }

    try {
      await habitsApi.setDayStatus(habitId, date, status)
    } catch (e) {
      h.history = prev
      throw e
    }
  }

  async function createHabit(request: HabitUpsertRequest) {
    const created = await habitsApi.createHabit(request)
    habits.value.push({ habit: created, history: [] })
  }

  async function updateHabit(id: string, request: HabitUpsertRequest) {
    const existing = habits.value.find(x => x.habit.id === id)
    if (!existing) return

    const backup = { ...existing.habit }
    Object.assign(existing.habit, request)

    try {
      const updated = await habitsApi.updateHabit(id, request)
      existing.habit = updated
    } catch (e) {
      existing.habit = backup
      throw e
    }
  }

  async function deleteHabit(id: string) {
    const idx = habits.value.findIndex(x => x.habit.id === id)
    if (idx === -1) return

    const backup = habits.value[idx]
    habits.value.splice(idx, 1)

    try {
      await habitsApi.deleteHabit(id)
    } catch (e) {
      if (backup) habits.value.splice(idx, 0, backup)
      throw e
    }
  }

  return {
    habits,
    habitsSorted,
    isLoading,
    rangeDays,
    fetchHabits,
    setDayStatus,
    createHabit,
    updateHabit,
    deleteHabit
  }
})

