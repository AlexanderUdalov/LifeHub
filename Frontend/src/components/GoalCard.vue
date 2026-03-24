<script setup lang="ts">
import { computed, ref } from 'vue'
import type { GoalDTO } from '@/api/GoalsAPI'
import Card from 'primevue/card'
import Button from 'primevue/button'
import Drawer from 'primevue/drawer'
import Carousel from 'primevue/carousel'
import Message from 'primevue/message'
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
import { getCurrentDayBasedStreakFallback, getCurrentWeeksStreak } from '@/utils/habitStreak'

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
  (e: 'complete-goal', goalId: string): void
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
const openTasksCount = computed(() => activeTasks.value.length)
const isCompleted = computed(() => !!props.goal.completedAt)

const MIN_STREAK_DAYS = 21

function normalizeNumber(value: number | string | null | undefined): number {
  const n = typeof value === 'number' ? value : Number(value ?? 0)
  return Number.isFinite(n) ? n : 0
}

function getHabitStreakDays(habit: HabitWithHistoryDTO): number {
  const goalNum = normalizeNumber(habit.habit.timesPerWeekGoal)
  if (goalNum >= 1 && goalNum <= 7) {
    const weeks = getCurrentWeeksStreak(habit.history, goalNum)
    return weeks * 7
  }

  const current = normalizeNumber(habit.currentStreak)
  if (current > 0) return current
  return getCurrentDayBasedStreakFallback(habit.history)
}

const habitsBelowThreshold = computed(() =>
  props.habits
    .map((habit) => ({
      id: habit.habit.id,
      title: habit.habit.title,
      streakDays: getHabitStreakDays(habit)
    }))
    .filter((item) => item.streakDays < MIN_STREAK_DAYS)
)

const addictionsBelowThreshold = computed(() =>
  props.addictions
    .map((addiction) => ({
      id: addiction.addiction.id,
      title: addiction.addiction.title,
      streakDays: normalizeNumber(addiction.currentStreakDays)
    }))
    .filter((item) => item.streakDays < MIN_STREAK_DAYS)
)

const hasOpenTasks = computed(() => openTasksCount.value > 0)
const hasShortHabits = computed(() => habitsBelowThreshold.value.length > 0)
const hasShortAddictions = computed(() => addictionsBelowThreshold.value.length > 0)
const hasCompletionWarnings = computed(
  () => hasOpenTasks.value || hasShortHabits.value || hasShortAddictions.value
)

const entityStats = computed(() => [
  { key: 'tasks', icon: 'pi pi-check-square', count: props.tasks.length, label: t('goals.tasksCount', { count: props.tasks.length }) },
  { key: 'habits', icon: 'pi pi-calendar', count: props.habits.length, label: t('goals.habitsCount', { count: props.habits.length }) },
  { key: 'addictions', icon: 'pi pi-ban', count: props.addictions.length, label: t('goals.addictionsCount', { count: props.addictions.length }) },
  {
    key: 'journal',
    icon: 'pi pi-book',
    count: journalEntriesSorted.value.length,
    label: t('goals.journalCount', { count: journalEntriesSorted.value.length })
  }
])

const expanded = ref(false)
const completedExpanded = ref(false)
const showCompleteDrawer = ref(false)
const completeStep = ref(0)

const completeSlides = computed(() => [
  {
    icon: 'pi pi-flag',
    title: t('goals.completeDrawer.steps.review.title'),
    text: t('goals.completeDrawer.steps.review.text')
  },
  {
    icon: 'pi pi-check-square',
    title: t('goals.completeDrawer.steps.tasks.title'),
    text: t('goals.completeDrawer.steps.tasks.text', { count: openTasksCount.value })
  },
  {
    icon: 'pi pi-calendar',
    title: t('goals.completeDrawer.steps.streak.title'),
    text: t('goals.completeDrawer.steps.streak.text')
  }
])

function toggle() {
  if (!hasEntities.value) return
  expanded.value = !expanded.value
}

function openCompleteDrawer() {
  completeStep.value = 0
  showCompleteDrawer.value = true
}

function closeCompleteDrawer() {
  showCompleteDrawer.value = false
}

function nextCompleteStep() {
  if (completeStep.value >= completeSlides.value.length - 1) return
  completeStep.value += 1
}

function prevCompleteStep() {
  if (completeStep.value <= 0) return
  completeStep.value -= 1
}

function confirmGoalCompletion() {
  emit('complete-goal', props.goal.id)
  closeCompleteDrawer()
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
        <div class="goal-header-actions">
          <Button v-if="!isCompleted" icon="pi pi-check" class="p-button-rounded goal-complete-btn" severity="success"
            :title="t('goals.completeDrawer.open')" :aria-label="t('goals.completeDrawer.open')"
            @click.stop="openCompleteDrawer" />
          <Button v-if="hasEntities" icon="pi pi-chevron-down" class="p-button-text p-button-rounded chevron"
            :class="{ rotated: expanded }" />
        </div>
      </div>
    </template>
    <template #content>
      <p v-if="goal.description" class="goal-description">
        {{ goal.description }}
      </p>

      <div v-if="!expanded" class="goal-stats">
        <span v-for="stat in entityStats" :key="stat.key" class="goal-stat-chip" :aria-label="stat.label" :title="stat.label">
          <i :class="stat.icon" aria-hidden="true" />
          <span>{{ stat.count }}</span>
        </span>
      </div>

      <div v-if="expanded" class="goal-expanded">
        <div v-if="tasks.length">
          <h4><i class="pi pi-check-square section-icon" aria-hidden="true" />{{ t('tasks.tasks') }}</h4>
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
          <h4><i class="pi pi-calendar section-icon" aria-hidden="true" />{{ t('habits.habits') }}</h4>
          <HabitCard v-for="habit in habits" :key="habit.habit.id" :habit="habit" no-border
            @edit="emit('edit-habit', $event)" />
        </div>

        <div v-if="addictions.length">
          <h4><i class="pi pi-ban section-icon" aria-hidden="true" />{{ t('addictions.addictions') }}</h4>
          <AddictionCard v-for="addiction in addictions" :key="addiction.addiction.id" :addiction="addiction" no-border
            @edit="emit('edit-addiction', $event)" />
        </div>

        <div v-if="journalEntriesSorted.length">
          <h4><i class="pi pi-book section-icon" aria-hidden="true" />{{ t('journal.entries') }}</h4>
          <JournalCard v-for="entry in journalEntriesSorted" :key="entry.id" :item="entry" no-border
            @edit-journal="emit('edit-journal', $event)" />
        </div>
      </div>
    </template>
  </Card>

  <Drawer v-model:visible="showCompleteDrawer" position="bottom" class="goal-complete-drawer"
    style="height: auto; max-height: 85vh">
    <template #header>
      <div class="goal-complete-title-row">
        <i class="pi pi-flag" />
        <span>{{ t('goals.completeDrawer.title', { goal: goal.title }) }}</span>
      </div>
    </template>

    <Carousel :value="completeSlides" :numVisible="1" :numScroll="1" :page="completeStep"
      @update:page="(value) => (completeStep = value)">
      <template #item="{ data }">
        <div class="goal-complete-slide">
          <i :class="data.icon" class="goal-complete-slide-icon" />
          <div class="goal-complete-slide-title">{{ data.title }}</div>
          <p class="goal-complete-slide-text">{{ data.text }}</p>
        </div>
      </template>
    </Carousel>

    <Message v-if="hasOpenTasks" severity="warn" :closable="false" class="goal-complete-warning">
      {{ t('goals.completeDrawer.warnings.openTasks', { count: openTasksCount }) }}
    </Message>
    <Message v-if="hasShortHabits" severity="warn" :closable="false" class="goal-complete-warning">
      {{ t('goals.completeDrawer.warnings.habitsLead') }}
      <ul class="goal-complete-list">
        <li v-for="habit in habitsBelowThreshold" :key="habit.id">
          {{ t('goals.completeDrawer.warnings.habitItem', { title: habit.title, days: habit.streakDays }) }}
        </li>
      </ul>
      <div>{{ t('goals.completeDrawer.warnings.habitsHint') }}</div>
    </Message>
    <Message v-if="hasShortAddictions" severity="warn" :closable="false" class="goal-complete-warning">
      {{ t('goals.completeDrawer.warnings.addictionsLead') }}
      <ul class="goal-complete-list">
        <li v-for="addiction in addictionsBelowThreshold" :key="addiction.id">
          {{
            t('goals.completeDrawer.warnings.addictionItem', { title: addiction.title, days: addiction.streakDays })
          }}
        </li>
      </ul>
      <div>{{ t('goals.completeDrawer.warnings.addictionsHint') }}</div>
    </Message>

    <template #footer>
      <div class="goal-complete-actions">
        <Button v-if="completeStep > 0" icon="pi pi-angle-left" class="p-button-text"
          :label="t('goals.completeDrawer.back')" @click="prevCompleteStep" />
        <span v-else />
        <Button v-if="completeStep < completeSlides.length - 1" icon="pi pi-angle-right" iconPos="right"
          :label="t('goals.completeDrawer.next')" @click="nextCompleteStep" />
        <Button v-else icon="pi pi-check" :severity="hasCompletionWarnings ? 'warn' : 'success'"
          :label="hasCompletionWarnings ? t('goals.completeDrawer.completeAnyway') : t('goals.completeDrawer.complete')"
          @click="confirmGoalCompletion" />
      </div>
    </template>
  </Drawer>
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
  transition: transform 0.2s ease;
}

.chevron.rotated {
  transform: rotate(180deg);
}

.goal-stats {
  display: flex;
  justify-content: space-between;
  gap: 0.5rem;
  margin-top: 0.75rem;
  font-size: 0.875rem;
  color: var(--p-text-muted-color);
}

.goal-stat-chip {
  display: flex;
  align-items: center;
  justify-content: center;
  flex: 1 1 0;
  gap: 0.35rem;
}

.goal-expanded {
  margin-top: 12px;
}

.goal-expanded h4 {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  margin: 12px 0 6px;
  font-size: 0.9rem;
  color: var(--p-text-muted-color);
}

.section-icon {
  font-size: 0.85rem;
}

.completed-tasks-section {
  margin-top: 8px;
}

.show-completed-btn,
.hide-completed-btn {
  justify-content: flex-start;
  color: var(--p-text-muted-color);
}

.goal-complete-title-row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-weight: 600;
}

.goal-complete-slide {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 0.5rem;
  padding: 1rem 0.75rem 1.25rem;
}

.goal-complete-slide-icon {
  font-size: 1.8rem;
  color: var(--p-primary-color);
}

.goal-complete-slide-title {
  font-weight: 600;
}

.goal-complete-slide-text {
  margin: 0;
  color: var(--p-text-muted-color);
}

.goal-complete-warning {
  margin-top: 0.75rem;
}

.goal-complete-list {
  margin: 0.5rem 0 0.5rem 1.25rem;
  padding: 0;
}

.goal-complete-actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
}

.goal-header-actions {
  margin-left: auto;
  display: flex;
  align-items: center;
}

.goal-complete-btn {
  margin-left: 0.25rem;
}
</style>
