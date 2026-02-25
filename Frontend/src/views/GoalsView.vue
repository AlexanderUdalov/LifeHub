<script setup lang="ts">
import { computed, onMounted } from 'vue'
import GoalCard from '@/components/GoalCard.vue'
import { useGoalsStore } from '@/stores/goals'
import { useTasksStore } from '@/stores/tasks'
import { useHabitsStore } from '@/stores/habits'
import { useAddictionsStore } from '@/stores/addictions'
import type { GoalItem } from '@/models/GoalItem'

const emit = defineEmits<{
  (e: 'edit-goal', goal: GoalItem): void
}>()

const goalsStore = useGoalsStore()
const tasksStore = useTasksStore()
const habitsStore = useHabitsStore()
const addictionsStore = useAddictionsStore()

onMounted(async () => {
  await Promise.all([
    goalsStore.fetchGoals(),
    tasksStore.fetchTasks(),
    habitsStore.fetchHabits(14),
    addictionsStore.fetchAddictions(60)
  ])
})

const tasksByGoalId = computed(() => {
  const grouped: Record<string, typeof tasksStore.tasks> = {}
  for (const task of tasksStore.tasks) {
    if (!task.goalId) continue
    grouped[task.goalId] ??= []
    const bucket = grouped[task.goalId]
    if (bucket) bucket.push(task)
  }
  return grouped
})

const habitsByGoalId = computed(() => {
  const grouped: Record<string, typeof habitsStore.habits> = {}
  for (const habit of habitsStore.habits) {
    if (!habit.habit.goalId) continue
    grouped[habit.habit.goalId] ??= []
    const bucket = grouped[habit.habit.goalId]
    if (bucket) bucket.push(habit)
  }
  return grouped
})

const addictionsByGoalId = computed(() => {
  const grouped: Record<string, typeof addictionsStore.addictions> = {}
  for (const addiction of addictionsStore.addictions) {
    if (!addiction.addiction.goalId) continue
    grouped[addiction.addiction.goalId] ??= []
    const bucket = grouped[addiction.addiction.goalId]
    if (bucket) bucket.push(addiction)
  }
  return grouped
})
</script>

<template>
  <div class="goals-view">
    <h1 class="view-page-header">{{ $t('goals.title') }}</h1>
    <GoalCard v-for="goal in goalsStore.goalsSorted" :key="goal.id" :goal="goal" :tasks="tasksByGoalId[goal.id] ?? []"
      :habits="habitsByGoalId[goal.id] ?? []" :addictions="addictionsByGoalId[goal.id] ?? []"
      @edit-goal="emit('edit-goal', $event)" />
  </div>
</template>

<style scoped>
.goals-view {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 0 12px 12px;
}

.view-page-header {
  font-size: var(--p-card-title-font-size);
  font-weight: 600;
  text-align: center;
}
</style>
