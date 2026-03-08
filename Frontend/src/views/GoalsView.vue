<script setup lang="ts">
defineOptions({ name: 'GoalsView' })
import { computed, onMounted } from 'vue'
import EmptyState from '@/components/EmptyState.vue'
import GoalCard from '@/components/GoalCard.vue'
import Skeleton from 'primevue/skeleton'
import { useGoalsStore } from '@/stores/goals'
import { useTasksStore } from '@/stores/tasks'
import { useHabitsStore } from '@/stores/habits'
import { useAddictionsStore } from '@/stores/addictions'
import type { GoalDTO } from '@/api/GoalsAPI'

const emit = defineEmits<{
  (e: 'edit-goal', goal: GoalDTO): void
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

    <div v-if="goalsStore.isLoading && goalsStore.goals.length === 0" class="goals-skeleton">
      <div v-for="i in 3" :key="i" class="skeleton-card">
        <Skeleton width="60%" height="1.5rem" class="skeleton-title" />
        <Skeleton width="100%" height="1rem" />
        <Skeleton width="90%" height="1rem" />
        <Skeleton width="70%" height="1rem" />
      </div>
    </div>

    <EmptyState v-else-if="goalsStore.goals.length === 0" icon="pi pi-flag"
      :title="$t('goals.empty')" :subtitle="$t('goals.emptySubtitle')" />

    <GoalCard v-else v-for="goal in goalsStore.goalsSorted" :key="goal.id" :goal="goal" :tasks="tasksByGoalId[goal.id] ?? []"
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

.goals-skeleton {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.skeleton-card {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  padding: 1rem;
  border-radius: var(--p-border-radius);
  background: var(--p-card-background);
  border: 1px solid var(--p-card-border-color);
}

.skeleton-title {
  margin-bottom: 0.25rem;
}

.view-page-header {
  font-size: var(--p-card-title-font-size);
  font-weight: 600;
  text-align: center;
}
</style>
