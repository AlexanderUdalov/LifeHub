<script setup lang="ts">
defineOptions({ name: 'GoalsView' })
import { computed, onActivated, onMounted, ref, watch } from 'vue'
import EmptyState from '@/components/EmptyState.vue'
import GoalCard from '@/components/GoalCard.vue'
import Skeleton from 'primevue/skeleton'
import { useGoalsStore } from '@/stores/goals'
import { useTasksStore } from '@/stores/tasks'
import { useHabitsStore } from '@/stores/habits'
import { useAddictionsStore } from '@/stores/addictions'
import { useNsfwContentStore } from '@/stores/nsfwContent'
import { useJournalStore } from '@/stores/journal'
import type { GoalDTO } from '@/api/GoalsAPI'
import type { JournalEntryDTO } from '@/api/JournalAPI'
import type { TaskDTO } from '@/api/TasksAPI'
import type { HabitDTO } from '@/api/HabitsAPI'
import type { AddictionDTO } from '@/api/AddictionsAPI'

const emit = defineEmits<{
  (e: 'edit-goal', goal: GoalDTO): void
  (e: 'edit-task', task: TaskDTO): void
  (e: 'edit-habit', habit: HabitDTO): void
  (e: 'edit-addiction', addiction: AddictionDTO): void
  (e: 'edit-journal', entry: JournalEntryDTO): void
}>()

const goalsStore = useGoalsStore()
const tasksStore = useTasksStore()
const habitsStore = useHabitsStore()
const addictionsStore = useAddictionsStore()
const nsfwContentStore = useNsfwContentStore()
const journalStore = useJournalStore()
const showCompletedGoals = ref(false)

onMounted(async () => {
  await Promise.all([
    goalsStore.fetchGoals(true),
    tasksStore.fetchTasks(),
    habitsStore.fetchHabits(56),
    addictionsStore.fetchAddictions(60),
    journalStore.loadEntries()
  ])
})

onActivated(async () => {
  await goalsStore.fetchGoals(true)
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
    if (!nsfwContentStore.addictionVisible(addiction.addiction)) continue
    if (!addiction.addiction.goalId) continue
    grouped[addiction.addiction.goalId] ??= []
    const bucket = grouped[addiction.addiction.goalId]
    if (bucket) bucket.push(addiction)
  }
  return grouped
})

const addictionsById = computed(
  () => new Map(addictionsStore.addictions.map((row) => [row.addiction.id, row.addiction]))
)

const entriesByGoalId = computed(() => {
  const grouped: Record<string, JournalEntryDTO[]> = {}
  for (const entry of journalStore.entries) {
    if (entry.addictionId) {
      const addiction = addictionsById.value.get(entry.addictionId)
      if (addiction && !nsfwContentStore.addictionVisible(addiction)) continue
    }
    if (!entry.goalId) continue
    let bucket = grouped[entry.goalId]
    if (!bucket) {
      bucket = []
      grouped[entry.goalId] = bucket
    }
    bucket.push(entry)
  }
  return grouped
})

const activeGoals = computed(() => goalsStore.goalsSorted.filter(goal => !goal.completedAt))
const completedGoals = computed(() => goalsStore.goalsSorted.filter(goal => !!goal.completedAt))

watch(completedGoals, (list) => {
  if (list.length === 0) showCompletedGoals.value = false
})

async function onCompleteGoal(goalId: string) {
  await goalsStore.completeGoal(goalId)
}
</script>

<template>
  <div class="goals-view">
    <h1 class="ds-page-header">{{ $t('goals.title') }}</h1>

    <div v-if="goalsStore.isLoading && goalsStore.goals.length === 0" class="goals-skeleton">
      <div v-for="i in 3" :key="i" class="skeleton-card">
        <Skeleton width="60%" height="1.5rem" class="skeleton-title" />
        <Skeleton width="100%" height="1rem" />
        <Skeleton width="90%" height="1rem" />
        <Skeleton width="70%" height="1rem" />
      </div>
    </div>

    <template v-else>
      <EmptyState v-if="activeGoals.length === 0" icon="pi pi-flag" :title="$t('goals.empty')"
        :subtitle="$t('goals.emptySubtitle')" />
      <GoalCard v-else v-for="goal in activeGoals" :key="goal.id" :goal="goal" :tasks="tasksByGoalId[goal.id] ?? []"
        :habits="habitsByGoalId[goal.id] ?? []" :addictions="addictionsByGoalId[goal.id] ?? []"
        :journal-entries="entriesByGoalId[goal.id] ?? []" @edit-goal="emit('edit-goal', $event)"
        @edit-task="emit('edit-task', $event)" @edit-habit="emit('edit-habit', $event)"
        @edit-addiction="emit('edit-addiction', $event)" @edit-journal="emit('edit-journal', $event)"
        @completion-change="tasksStore.toggleTaskCompletion" @complete-goal="onCompleteGoal" />

      <div v-if="completedGoals.length" class="completed-goals-text-block">
        <button class="completed-goals-toggle" type="button" @click="showCompletedGoals = !showCompletedGoals">
          <i :class="showCompletedGoals ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"/>
          {{
            showCompletedGoals
              ? $t('goals.hideCompletedGoalsInline', { count: completedGoals.length })
              : $t('goals.showCompletedGoalsInline', { count: completedGoals.length })
          }}
        </button>
        <ul v-if="showCompletedGoals" class="completed-goals-list">
          <li v-for="goal in completedGoals" :key="goal.id">
            {{ goal.title }}
          </li>
        </ul>
      </div>
    </template>
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

.completed-goals-text-block {
  margin-top: 0.25rem;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.completed-goals-toggle {
  display: inline-flex;
  align-items: center;
  gap: 0.2rem;
  border: none;
  background: transparent;
  padding: 0;
  color: var(--p-text-muted-color);
  font-size: 0.875rem;
  cursor: pointer;
  text-align: center;
}

.completed-goals-list {
  margin: 0.5rem 0 0;
  color: var(--p-text-muted-color);
  font-size: 0.9rem;
  padding-left: 1.1rem;
}
</style>
