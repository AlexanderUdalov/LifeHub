<script setup lang="ts">
import { computed, ref } from 'vue'
import type { GoalDTO } from '@/api/GoalsAPI'
import Card from 'primevue/card'
import Button from 'primevue/button'
import TaskCard from './TaskCard.vue'
import HabitCard from './HabitCard.vue'
import AddictionCard from './AddictionCard.vue'
import JournalCard from './JournalCard.vue'
import type { HabitDTO, HabitWithHistoryDTO } from '@/api/HabitsAPI'
import type { AddictionDTO, AddictionWithResetsDTO } from '@/api/AddictionsAPI'
import type { TaskDTO } from '@/api/TasksAPI'
import type { JournalEntryDTO } from '@/api/JournalAPI'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { useI18n } from 'vue-i18n'
import { useDeadlineFormatter } from '@/composables/useDeadlineFormatter'

const props = withDefaults(
  defineProps<{
    goal: GoalDTO
    tasks: TaskDTO[]
    habits: HabitWithHistoryDTO[]
    addictions: AddictionWithResetsDTO[]
    journalEntries?: JournalEntryDTO[]
  }>(),
  { journalEntries: () => [] }
)

const emit = defineEmits<{
  (e: 'edit-goal', goal: GoalDTO): void
  (e: 'edit-task', task: TaskDTO): void
  (e: 'edit-habit', habit: HabitDTO): void
  (e: 'edit-addiction', addiction: AddictionDTO): void
  (e: 'edit-journal', entry: JournalEntryDTO): void
  (e: 'completion-change', taskId: string, value: boolean): void
}>()

const { t } = useI18n()
const { formatDeadline } = useDeadlineFormatter()
const lifeAreasStore = useLifeAreasStore()
const areaColor = computed(() => lifeAreasStore.getAreaColorById(props.goal.lifeAreaId))
const cardBorderStyle = computed(() =>
  areaColor.value
    ? { borderLeftWidth: '4px', borderLeftStyle: 'solid', borderLeftColor: areaColor.value }
    : { borderLeftWidth: 0 }
)

const dueDateText = computed(() => formatDeadline(new Date(props.goal.dueDate)))

const isOverdue = computed(() => {
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  const due = new Date(props.goal.dueDate)
  due.setHours(0, 0, 0, 0)
  return due < today
})

const isSoon = computed(() => {
  if (isOverdue.value) return false
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  const due = new Date(props.goal.dueDate)
  due.setHours(0, 0, 0, 0)
  const msPerDay = 24 * 60 * 60 * 1000
  const daysLeft = Math.ceil((due.getTime() - today.getTime()) / msPerDay)
  return daysLeft >= 0 && daysLeft <= 7
})

const hasEntities = computed(
  () =>
    props.tasks.length > 0 ||
    props.habits.length > 0 ||
    props.addictions.length > 0 ||
    (props.journalEntries?.length ?? 0) > 0
)

const activeTasks = computed(() => props.tasks.filter(t => !t.completionDate))

const journalEntriesSorted = computed(() =>
  [...(props.journalEntries ?? [])].sort(
    (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
  )
)
const completedTasks = computed(() =>
  props.tasks.filter(t => t.completionDate).sort(
    (a, b) => new Date(b.completionDate!).getTime() - new Date(a.completionDate!).getTime()
  )
)

const expanded = ref(false)
const completedExpanded = ref(false)

function toggle() {
  if (!hasEntities.value) return
  expanded.value = !expanded.value
}
</script>

<template>
  <Card class="goal-card" :style="cardBorderStyle">
    <template #header>
      <div class="goal-header-row" :class="{ 'has-entities': hasEntities }" @click="toggle">
        <div class="goal-title-wrap">
          <div class="goal-title" @click.stop="emit('edit-goal', goal)">
            {{ goal.title }}
          </div>
          <div class="goal-due" :class="{ soon: isSoon, overdue: isOverdue }">
            {{ dueDateText }}
          </div>
        </div>
        <Button v-if="hasEntities" icon="pi pi-chevron-down" class="p-button-text p-button-rounded chevron"
          :class="{ rotated: expanded }" />
      </div>
    </template>
    <template #content>
      <p v-if="goal.description" class="goal-description">
        {{ goal.description }}
      </p>

      <div v-if="!expanded" class="goal-stats">
        <span>{{ t('goals.tasksCount', { count: tasks.length }) }}</span>
        <span>{{ t('goals.habitsCount', { count: habits.length }) }}</span>
        <span>{{ t('goals.addictionsCount', { count: addictions.length }) }}</span>
        <span>{{ t('goals.journalCount', { count: journalEntriesSorted.length }) }}</span>
      </div>

      <div v-if="expanded" class="goal-expanded">
        <div v-if="tasks.length">
          <h4>{{ t('tasks.tasks') }}</h4>
          <TaskCard v-for="task in activeTasks" :key="task.id" :task="task" no-border @edit="emit('edit-task', task)"
            @completion-change="(id, val) => emit('completion-change', id, val)" />
          <div v-if="completedTasks.length" class="completed-tasks-section">
            <Button v-if="!completedExpanded" class="show-completed-btn p-button-text"
              :label="t('goals.showCompleted', { count: completedTasks.length })" icon="pi pi-chevron-down"
              @click="completedExpanded = true" />
            <template v-else>
              <Button class="hide-completed-btn p-button-text" :label="t('goals.hideCompleted')" icon="pi pi-chevron-up"
                @click="completedExpanded = false" />
              <TaskCard v-for="task in completedTasks" :key="task.id" :task="task" no-border
                @edit="emit('edit-task', task)" @completion-change="(id, val) => emit('completion-change', id, val)" />
            </template>
          </div>
        </div>

        <div v-if="habits.length">
          <h4>{{ t('habits.habits') }}</h4>
          <HabitCard v-for="habit in habits" :key="habit.habit.id" :habit="habit" no-border
            @edit="emit('edit-habit', $event)" />
        </div>

        <div v-if="addictions.length">
          <h4>{{ t('addictions.addictions') }}</h4>
          <AddictionCard v-for="addiction in addictions" :key="addiction.addiction.id" :addiction="addiction" no-border
            @edit="emit('edit-addiction', $event)" />
        </div>

        <div v-if="journalEntriesSorted.length">
          <h4>{{ t('journal.entries') }}</h4>
          <JournalCard v-for="entry in journalEntriesSorted" :key="entry.id" :item="entry" no-border
            @edit-journal="emit('edit-journal', $event)" />
        </div>
      </div>
    </template>
  </Card>
</template>

<style scoped>
.goal-card {
  border-left-width: 4px;
  border-left-style: solid;
  border-radius: 16px;
}

.goal-header-row {
  display: flex;
  padding-top: 0.5rem;
  padding-left: 1rem;
  padding-right: 1rem;
}

.goal-header-row.has-entities {
  cursor: pointer;
}

.goal-title-wrap {
  min-width: 0;
}

.goal-title {
  font-weight: 600;
  cursor: pointer;
  user-select: none;
}

.goal-due {
  font-size: 0.875rem;
  color: var(--p-text-muted-color);
}

.goal-due.soon {
  color: var(--p-orange-400);
}

.goal-due.overdue {
  color: var(--p-red-600);
}

.goal-description {
  font-size: 0.875rem;
  color: var(--p-text-muted-color);
  margin: 0.5rem 0 0;
  white-space: pre-wrap;
  word-break: break-word;
}

.chevron {
  margin-left: auto;
  transition: transform 0.2s ease;
}

.chevron.rotated {
  transform: rotate(180deg);
}

.goal-stats {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
  margin-top: 0.75rem;
  font-size: 0.875rem;
  color: var(--p-text-muted-color);
}

.goal-expanded {
  margin-top: 12px;
}

.goal-expanded h4 {
  margin: 12px 0 6px;
  font-size: 0.9rem;
  color: var(--p-text-muted-color);
}

.completed-tasks-section {
  margin-top: 8px;
}

.show-completed-btn,
.hide-completed-btn {
  justify-content: flex-start;
  color: var(--p-text-muted-color);
}
</style>
