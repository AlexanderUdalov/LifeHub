import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import {
  goalsApi,
  type CreateGoalRequest,
  type GoalDTO,
  type UpdateGoalRequest
} from '@/api/GoalsAPI'

export const useGoalsStore = defineStore('goals', () => {
  const goals = ref<GoalDTO[]>([])
  const isLoading = ref(false)

  const goalsSorted = computed(() =>
    [...goals.value].sort((a, b) => {
      const aCompleted = !!a.completedAt
      const bCompleted = !!b.completedAt
      if (aCompleted !== bCompleted) return aCompleted ? 1 : -1
      if (aCompleted && bCompleted) {
        return new Date(b.completedAt!).getTime() - new Date(a.completedAt!).getTime()
      }
      return new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime()
    })
  )

  function getGoalById(goalId: string | null | undefined): GoalDTO | null {
    if (!goalId) return null
    return goals.value.find(x => x.id === goalId) ?? null
  }

  async function fetchGoals(includeCompleted = false) {
    isLoading.value = true
    try {
      goals.value = await goalsApi.getGoals(includeCompleted)
    } finally {
      isLoading.value = false
    }
  }

  async function createGoal(request: CreateGoalRequest) {
    const created = await goalsApi.createGoal(request)
    goals.value.push(created)
  }

  async function updateGoal(id: string, request: UpdateGoalRequest) {
    const existing = goals.value.find(x => x.id === id)
    if (!existing) return

    const backup = { ...existing }
    Object.assign(existing, request)

    try {
      const updated = await goalsApi.updateGoal(id, request)
      Object.assign(existing, updated)
    } catch (e) {
      Object.assign(existing, backup)
      throw e
    }
  }

  async function deleteGoal(id: string) {
    const idx = goals.value.findIndex(x => x.id === id)
    if (idx === -1) return

    const backup = goals.value[idx]
    goals.value.splice(idx, 1)

    try {
      await goalsApi.deleteGoal(id)
    } catch (e) {
      if (backup) goals.value.splice(idx, 0, backup)
      throw e
    }
  }

  async function completeGoal(id: string) {
    const existing = goals.value.find(x => x.id === id)
    if (!existing) return

    const backup = { ...existing }
    existing.completedAt = new Date().toISOString()

    try {
      const updated = await goalsApi.completeGoal(id)
      Object.assign(existing, updated)
    } catch (e) {
      Object.assign(existing, backup)
      throw e
    }
  }

  return {
    goals,
    goalsSorted,
    isLoading,
    fetchGoals,
    createGoal,
    updateGoal,
    deleteGoal,
    completeGoal,
    getGoalById
  }
})
