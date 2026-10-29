<script setup lang="ts">
defineOptions({ name: 'TasksView' })
import { onMounted, onUnmounted, computed, ref, watch } from 'vue'

import Accordion from 'primevue/accordion'
import AccordionPanel from 'primevue/accordionpanel'
import AccordionHeader from 'primevue/accordionheader'
import AccordionContent from 'primevue/accordioncontent'
import Badge from 'primevue/badge'
import Button from 'primevue/button'
import DatePicker from 'primevue/datepicker'
import Skeleton from 'primevue/skeleton'
import EmptyState from '@/components/EmptyState.vue'
import TaskCard from '@/components/TaskCard.vue'
import { getCompletedTasks, type TaskDTO } from '@/api/TasksAPI'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { useI18n } from 'vue-i18n'
import { useTasksStore } from '@/stores/tasks'
import {
  getStoredTaskViewMode,
  setStoredTaskViewMode,
  type TaskViewMode,
} from '@/utils/taskViewMode'
import { isToday, isSameDateOnly, startOfDay, toDateOnlyString } from '@/utils/dateOnly'

const { t, locale } = useI18n()
const emit = defineEmits<{
  (e: 'edit-task', task: TaskDTO | null): void
  (e: 'create-task', date: string): void
}>()

const tasksStore = useTasksStore()
const scope = ref('today')
const scopeOptions = computed(() => [
  { value: 'today', label: t('tasks.list.today') },
  { value: 'overdue', label: t('tasks.list.overdue') + ' · ' + tasksStore.overdueTasks.length },
  { value: 'thisweek', label: t('tasks.list.thisweek') },
  { value: 'all', label: t('redesign.all') },
  { value: 'inbox', label: t('tasks.list.inbox') },
  { value: 'completed', label: t('tasks.list.completed') },
])
function sectionVisible(sectionKey: string) {
  return (
    scope.value === 'all' ||
    scope.value === sectionKey ||
    (scope.value === 'today' && sectionKey === 'overdue')
  )
}
const calendarCompleted = ref(false)
const calendarHistory = ref<TaskDTO[]>([])
const calendarLoading = ref(false)
const calendarError = ref(false)
let calendarVersion = 0
const lifeAreasStore = useLifeAreasStore()

onMounted(() => {
  tasksStore.fetchTasks()
})

const taskViewMode = ref<TaskViewMode>(getStoredTaskViewMode() ?? 'standard')

const taskViewModeOptions = computed(() => [
  {
    label: t('profile-view.task-list-view-standard'),
    value: 'standard' as TaskViewMode,
    icon: 'pi pi-bars',
  },
  {
    label: t('profile-view.task-list-view-compact'),
    value: 'compact' as TaskViewMode,
    icon: 'pi pi-align-justify',
  },
  {
    label: t('profile-view.task-list-view-calendar'),
    value: 'calendar' as TaskViewMode,
    icon: 'pi pi-calendar',
  },
])

function onTaskViewModeChange(mode: TaskViewMode | null | undefined) {
  if (!mode) {
    return
  }
  setStoredTaskViewMode(mode)
  taskViewMode.value = mode
}

const taskSections = computed(() => [
  {
    key: 'overdue',
    title: t('tasks.list.overdue'),
    tasks: tasksStore.overdueTasks,
    draggable: false,
  },
  { key: 'today', title: t('tasks.list.today'), tasks: tasksStore.todayTasks, draggable: true },
  {
    key: 'thisweek',
    title: t('tasks.list.thisweek'),
    tasks: tasksStore.weekTasks,
    draggable: true,
  },
  { key: 'inbox', title: t('tasks.list.inbox'), tasks: tasksStore.inboxTasks, draggable: true },
  {
    key: 'later',
    title: t('redesign.later'),
    tasks: tasksStore.tasks.filter(
      (task) =>
        !tasksStore.todayTasks.includes(task) &&
        !tasksStore.overdueTasks.includes(task) &&
        !tasksStore.weekTasks.includes(task) &&
        !!task.dueDate,
    ),
    draggable: true,
  },
  {
    key: 'completed',
    title: t('tasks.list.completed'),
    tasks: [] as TaskDTO[],
    draggable: false,
    isCompleted: true,
  },
])

/** Keys (by panel index) for the panels that are actually rendered via `v-if`. */
const visibleSectionKeys = computed(() => {
  const keys: string[] = []
  for (const [index, section] of taskSections.value.entries()) {
    const isVisible =
      section.tasks.length > 0 || (section.isCompleted && tasksStore.completedTotal > 0)
    if (isVisible && sectionVisible(section.key)) keys.push(String(index))
  }
  return keys
})

const activeAccordionValue = ref<string[]>([])

// Ensure at least one visible panel is opened (e.g. when panel "0" isn't rendered).
watch(
  visibleSectionKeys,
  (keys) => {
    const currentlyVisible = activeAccordionValue.value.filter((k) => keys.includes(k))
    if (!keys.length) {
      activeAccordionValue.value = []
      return
    }

    if (!currentlyVisible.length) {
      activeAccordionValue.value = [...keys]
    }
  },
  { immediate: true },
)

watch(scope, () => {
  activeAccordionValue.value = taskSections.value.flatMap((section, index) =>
    sectionVisible(section.key) ? [String(index)] : [],
  )
})
const hasScopeTasks = computed(() =>
  taskSections.value.some(
    (section) =>
      sectionVisible(section.key) &&
      (section.tasks.length || (section.isCompleted && tasksStore.completedTotal)),
  ),
)
const hasAnyTasks = computed(() => tasksStore.tasks.length > 0 || tasksStore.completedTotal > 0)

const COMPLETED_PAGE_SIZE = 20

/** Loaded completed tasks in order (up to first undefined slot). */
const completedLoadedList = computed(() => {
  const list = tasksStore.completedTasks
  const i = list.findIndex((t) => t === undefined)
  return i < 0 ? (list as TaskDTO[]) : (list.slice(0, i) as TaskDTO[])
})

const firstUnloadedCompletedIndex = computed(() => {
  const i = tasksStore.completedTasks.findIndex((t) => t === undefined)
  return i < 0 ? tasksStore.completedTotal : i
})

const hasMoreCompleted = computed(
  () =>
    tasksStore.completedTotal > 0 && firstUnloadedCompletedIndex.value < tasksStore.completedTotal,
)

const loadMoreCompletedSentinel = ref<HTMLElement | null>(null)

async function loadMoreCompleted() {
  if (tasksStore.isLoadingMoreCompleted || !hasMoreCompleted.value) return
  const first = firstUnloadedCompletedIndex.value
  const last = Math.min(first + COMPLETED_PAGE_SIZE - 1, tasksStore.completedTotal - 1)
  if (first > last) return
  await tasksStore.fetchMoreCompletedTasks(first, last)
}

function isSentinelNearViewport(): boolean {
  const el = loadMoreCompletedSentinel.value
  if (!el) return false
  const rect = el.getBoundingClientRect()
  const viewportHeight = window.innerHeight || document.documentElement.clientHeight
  // Keep loading while sentinel stays near viewport bottom.
  return rect.top <= viewportHeight + 100
}

async function loadMoreCompletedUntilNotVisible() {
  while (!tasksStore.isLoadingMoreCompleted && hasMoreCompleted.value && isSentinelNearViewport()) {
    await loadMoreCompleted()
  }
}

let loadMoreObserver: IntersectionObserver | null = null

onMounted(() => {
  loadMoreObserver = new IntersectionObserver(
    (entries) => {
      if (!entries[0]?.isIntersecting) return
      void loadMoreCompletedUntilNotVisible()
    },
    { root: null, rootMargin: '100px', threshold: 0 },
  )
  watch(
    loadMoreCompletedSentinel,
    (el, prev) => {
      if (prev && loadMoreObserver) loadMoreObserver.unobserve(prev)
      if (el && loadMoreObserver) loadMoreObserver.observe(el)
    },
    { immediate: true },
  )
})

onUnmounted(() => {
  if (loadMoreObserver && loadMoreCompletedSentinel.value) {
    loadMoreObserver.unobserve(loadMoreCompletedSentinel.value)
  }
  loadMoreObserver?.disconnect()
})

function onEditTask(task: TaskDTO) {
  emit('edit-task', task)
}

const selectedDate = ref<Date>(startOfDay(new Date()))

async function loadCalendarHistory() {
  const version = ++calendarVersion
  calendarHistory.value = []
  calendarError.value = false
  calendarLoading.value = calendarCompleted.value
  if (!calendarCompleted.value) return
  try {
    let offset = 0,
      total = 1
    const loaded: TaskDTO[] = []
    while (offset < total) {
      const page = await getCompletedTasks(100, offset, toDateOnlyString(selectedDate.value))
      if (version !== calendarVersion) return
      loaded.push(...page.items)
      total = page.total
      offset += page.items.length
      if (!page.items.length) break
    }
    calendarHistory.value = loaded
    tasksStore.rememberHistory(loaded)
  } catch {
    if (version === calendarVersion) calendarError.value = true
  } finally {
    if (version === calendarVersion) calendarLoading.value = false
  }
}
watch(
  [selectedDate, calendarCompleted, () => tasksStore.tasks, () => tasksStore.completedTotal],
  loadCalendarHistory,
)

const calendarTaskDateKeys = computed(() => {
  const keys = new Set<string>()
  for (const task of tasksStore.tasks) {
    if (task.completionDate) continue
    if (task.dueDate) keys.add(toDateOnlyString(new Date(task.dueDate)))
  }
  if (tasksStore.inboxTasks.length) keys.add(toDateOnlyString(new Date()))
  return keys
})

const calendarTasksForSelectedDay = computed(() => {
  const selected = startOfDay(selectedDate.value)
  return [...tasksStore.tasks, ...(calendarCompleted.value ? calendarHistory.value : [])]
    .filter((task) =>
      task.dueDate ? isSameDateOnly(new Date(task.dueDate), selected) : isToday(selected),
    )
    .sort((a, b) => Number(a.sortOrder ?? Infinity) - Number(b.sortOrder ?? Infinity))
})

function calendarDayColors(date: { year: number; month: number; day: number }): string[] {
  const key = calendarCellKey(date)
  return [
    ...new Set(
      tasksStore.tasks
        .filter(
          (task) =>
            task.dueDate?.slice(0, 10) === key ||
            (!task.dueDate && key === toDateOnlyString(new Date())),
        )
        .map((task) => lifeAreasStore.getAreaColorById(task.lifeAreaId) ?? 'var(--app-accent)'),
    ),
  ].slice(0, 3)
}

function calendarCellKey(date: { year: number; month: number; day: number }): string {
  return `${date.year}-${String(date.month + 1).padStart(2, '0')}-${String(date.day).padStart(2, '0')}`
}

const dropSectionKey = ref<string | null>(null)
const dropIndex = ref(0)

function onDragStart(sectionKey: string, taskIndex: number, _event: PointerEvent) {
  if (!(_event.target instanceof HTMLElement)) return

  const container = _event.target.closest('[data-draggable-list]')
  if (!container) return

  const section = taskSections.value.find((s) => s.key === sectionKey)
  if (!section) return

  const rows = Array.from(container.querySelectorAll('[data-task-row]')) as HTMLElement[]
  if (!rows.length) return

  dropSectionKey.value = sectionKey
  dropIndex.value = taskIndex

  function getTargetIndex(clientY: number): number {
    for (let i = 0; i < rows.length; i++) {
      const rect = rows[i]!.getBoundingClientRect()
      const mid = rect.top + rect.height / 2
      if (clientY < mid) return i
    }
    return rows.length
  }

  function onPointerMove(e: PointerEvent) {
    if (dropSectionKey.value !== sectionKey) return
    dropIndex.value = getTargetIndex(e.clientY)
  }

  function cleanup() {
    document.removeEventListener('pointermove', onPointerMove)
    document.removeEventListener('pointerup', onPointerUp)
    document.removeEventListener('pointercancel', cleanup)
    document.body.style.cursor = ''
    document.body.style.touchAction = ''
  }

  async function onPointerUp(e: PointerEvent) {
    cleanup()

    const toIndex = getTargetIndex(e.clientY)
    if (toIndex === fromIndex) {
      dropSectionKey.value = null
      return
    }

    const newOrder = [...section!.tasks]
    const [removed] = newOrder.splice(fromIndex, 1)
    if (!removed) {
      dropSectionKey.value = null
      return
    }
    const insertIndex = fromIndex < toIndex ? toIndex - 1 : toIndex
    newOrder.splice(insertIndex, 0, removed)

    if (sectionKey === 'thisweek') {
      const reference = insertIndex > 0 ? newOrder[insertIndex - 1] : newOrder[insertIndex + 1]
      const refDate = reference?.dueDate ?? null
      if (refDate != null && refDate !== removed.dueDate) {
        await tasksStore.editTask(removed.id, {
          title: removed.title,
          description: removed.description,
          dueDate: refDate,
          completionDate: removed.completionDate,
          recurrenceRule: removed.recurrenceRule,
          goalId: removed.goalId,
          lifeAreaId: removed.lifeAreaId,
          sortOrder: removed.sortOrder,
        })
      }
    }

    await tasksStore.reorderTasksInList(newOrder)
    dropSectionKey.value = null
  }

  const fromIndex = taskIndex
  document.body.style.cursor = 'grabbing'
  document.body.style.touchAction = 'none'
  document.addEventListener('pointermove', onPointerMove)
  document.addEventListener('pointerup', onPointerUp)
  document.addEventListener('pointercancel', cleanup)
}
</script>

<template>
  <div class="tasks-view-root">
    <header class="tasks-view-header">
      <h1 class="ds-page-header">{{ $t('tasks.tasks') }}</h1>
      <div class="tasks-view-actions">
        <Button
          :label="$t('tasks.editdialog.newTask')"
          icon="pi pi-plus"
          class="desktop-create-btn"
          @click="emit('edit-task', null)"
        />
        <div class="task-view-switch" role="group" :aria-label="t('profile-view.task-list-view')">
          <button
            v-for="option in taskViewModeOptions"
            :key="option.value"
            type="button"
            :aria-label="option.label"
            :aria-pressed="taskViewMode === option.value"
            @click="onTaskViewModeChange(option.value)"
          >
            <i :class="option.icon" aria-hidden="true" />
          </button>
        </div>
      </div>
    </header>

    <div class="task-scope-row">
      <select
        v-if="taskViewMode !== 'calendar'"
        v-model="scope"
        class="native-select"
        :aria-label="t('redesign.taskScope')"
      >
        <option v-for="option in scopeOptions" :key="option.value" :value="option.value">
          {{ option.label }}
        </option></select
      ><label v-else class="muted"
        ><input v-model="calendarCompleted" type="checkbox" />
        {{ t('tasks.list.completed') }}</label
      >
    </div>
    <div v-if="tasksStore.isLoading && tasksStore.tasks.length === 0" class="tasks-skeleton">
      <Skeleton v-for="i in 4" :key="i" height="3.5rem" class="skeleton-row" />
    </div>

    <EmptyState
      v-else-if="!hasAnyTasks"
      icon="pi pi-list-check"
      :title="$t('tasks.empty')"
      :subtitle="$t('tasks.emptySubtitle')"
    />

    <EmptyState
      v-else-if="taskViewMode !== 'calendar' && !hasScopeTasks"
      icon="pi pi-check-square"
      :title="t('redesign.noTasksInScope')"
    />
    <Accordion
      v-else-if="taskViewMode === 'standard'"
      v-model:value="activeAccordionValue"
      multiple
    >
      <template v-for="(section, index) in taskSections" :key="section.key">
        <AccordionPanel
          v-if="
            sectionVisible(section.key) &&
            (section.tasks.length || (section.isCompleted && tasksStore.completedTotal > 0))
          "
          :value="String(index)"
          class="tasks-list"
        >
          <AccordionHeader>
            <div class="tasks-list-header">
              <span>{{ section.title }}</span>
              <Badge>{{
                section.isCompleted ? tasksStore.completedTotal : section.tasks.length
              }}</Badge>
            </div>
          </AccordionHeader>

          <AccordionContent>
            <template v-if="section.isCompleted">
              <div class="completed-list">
                <TransitionGroup name="task-list">
                  <div v-for="task in completedLoadedList" :key="task.id" class="task-card-wrapper">
                    <TaskCard
                      class="task-card"
                      :task="task"
                      @completion-change="tasksStore.toggleTaskCompletion"
                      @edit="onEditTask"
                    />
                  </div>
                  <div
                    v-if="tasksStore.isLoadingMoreCompleted"
                    class="task-card-wrapper completed-loading"
                  >
                    <Skeleton height="3rem" class="skeleton-row" />
                  </div>
                  <div
                    v-show="hasMoreCompleted && !tasksStore.isLoadingMoreCompleted"
                    ref="loadMoreCompletedSentinel"
                    class="completed-load-more-sentinel"
                    aria-hidden="true"
                  />
                </TransitionGroup>
              </div>
            </template>
            <TransitionGroup v-else name="task-list">
              <template v-if="section.draggable">
                <div data-draggable-list>
                  <div v-for="(task, taskIndex) in section.tasks" :key="task.id" data-task-row>
                    <div
                      class="drop-indicator"
                      :class="{ active: dropSectionKey === section.key && dropIndex === taskIndex }"
                    />
                    <TaskCard
                      class="task-card"
                      :task="task"
                      :draggable="true"
                      @completion-change="tasksStore.toggleTaskCompletion"
                      @edit="onEditTask"
                      @drag-start="(e) => onDragStart(section.key, taskIndex, e)"
                    />
                  </div>
                  <div
                    class="drop-indicator drop-indicator-last"
                    :class="{
                      active: dropSectionKey === section.key && dropIndex === section.tasks.length,
                    }"
                  />
                </div>
              </template>
              <template v-else>
                <TaskCard
                  v-for="task in section.tasks"
                  :key="task.id"
                  :task="task"
                  @completion-change="tasksStore.toggleTaskCompletion"
                  @edit="onEditTask"
                />
              </template>
            </TransitionGroup>
          </AccordionContent>
        </AccordionPanel>
      </template>
    </Accordion>

    <Accordion v-else-if="taskViewMode === 'compact'" v-model:value="activeAccordionValue" multiple>
      <template v-for="(section, index) in taskSections" :key="section.key">
        <AccordionPanel
          v-if="
            sectionVisible(section.key) &&
            (section.tasks.length || (section.isCompleted && tasksStore.completedTotal > 0))
          "
          :value="String(index)"
          class="tasks-list"
        >
          <AccordionHeader>
            <div class="tasks-list-header">
              <span>{{ section.title }}</span>
              <Badge>{{
                section.isCompleted ? tasksStore.completedTotal : section.tasks.length
              }}</Badge>
            </div>
          </AccordionHeader>

          <AccordionContent>
            <template v-if="section.isCompleted">
              <div class="completed-list">
                <TransitionGroup name="task-list">
                  <div v-for="task in completedLoadedList" :key="task.id" class="task-card-wrapper">
                    <TaskCard
                      class="task-card"
                      :task="task"
                      :compact="true"
                      :hide-goal="true"
                      @completion-change="tasksStore.toggleTaskCompletion"
                      @edit="onEditTask"
                    />
                  </div>
                  <div
                    v-if="tasksStore.isLoadingMoreCompleted"
                    class="task-card-wrapper completed-loading"
                  >
                    <Skeleton height="2.5rem" class="skeleton-row" />
                  </div>
                  <div
                    v-show="hasMoreCompleted && !tasksStore.isLoadingMoreCompleted"
                    ref="loadMoreCompletedSentinel"
                    class="completed-load-more-sentinel"
                    aria-hidden="true"
                  />
                </TransitionGroup>
              </div>
            </template>
            <TransitionGroup v-else name="task-list">
              <template v-if="section.draggable">
                <div data-draggable-list>
                  <div v-for="(task, taskIndex) in section.tasks" :key="task.id" data-task-row>
                    <div
                      class="drop-indicator"
                      :class="{ active: dropSectionKey === section.key && dropIndex === taskIndex }"
                    />
                    <TaskCard
                      class="task-card"
                      :task="task"
                      :draggable="true"
                      :compact="true"
                      :hide-deadline="section.key === 'today'"
                      :hide-goal="true"
                      @completion-change="tasksStore.toggleTaskCompletion"
                      @edit="onEditTask"
                      @drag-start="(e) => onDragStart(section.key, taskIndex, e)"
                    />
                  </div>
                  <div
                    class="drop-indicator drop-indicator-last"
                    :class="{
                      active: dropSectionKey === section.key && dropIndex === section.tasks.length,
                    }"
                  />
                </div>
              </template>
              <template v-else>
                <TaskCard
                  v-for="task in section.tasks"
                  :key="task.id"
                  :task="task"
                  :compact="true"
                  :hide-deadline="section.key === 'today'"
                  :hide-goal="true"
                  @completion-change="tasksStore.toggleTaskCompletion"
                  @edit="onEditTask"
                />
              </template>
            </TransitionGroup>
          </AccordionContent>
        </AccordionPanel>
      </template>
    </Accordion>

    <div v-else-if="taskViewMode === 'calendar'" class="calendar-view">
      <div class="calendar-view-top">
        <DatePicker v-model="selectedDate" inline class="calendar-picker">
          <template #date="{ date }">
            <span
              class="calendar-day"
              :class="{ 'has-tasks': calendarTaskDateKeys.has(calendarCellKey(date)) }"
            >
              {{ date.day
              }}<span class="calendar-area-dots"
                ><span
                  v-for="color in calendarDayColors(date)"
                  :key="color"
                  :style="{ background: color }"
              /></span>
            </span>
          </template>
        </DatePicker>
      </div>

      <div class="calendar-view-bottom">
        <div class="calendar-day-header">
          <span>{{
            selectedDate.toLocaleDateString(locale, { day: 'numeric', month: 'long' })
          }}</span>
          <Badge>{{ calendarTasksForSelectedDay.length }}</Badge>
        </div>

        <p v-if="calendarLoading" class="muted">{{ t('dayFeedback.loading') }}</p>
        <p v-if="calendarError" class="muted">
          {{ t('redesign.loadError') }}
          <button type="button" class="text-action" @click="loadCalendarHistory">
            {{ t('dayFeedback.retry') }}
          </button>
        </p>
        <TransitionGroup name="task-list">
          <TaskCard
            v-for="task in calendarTasksForSelectedDay"
            :key="task.id"
            :task="task"
            @completion-change="tasksStore.toggleTaskCompletion"
            @edit="onEditTask"
          />
        </TransitionGroup>
        <button
          type="button"
          class="calendar-inline-add"
          @click="emit('create-task', toDateOnlyString(selectedDate))"
        >
          <span><i class="pi pi-plus" /></span>{{ t('redesign.addTask') }}
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.tasks-view-root {
  width: 100%;
}

.tasks-skeleton {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  padding: 0 12px;
}

.skeleton-row {
  border-radius: var(--p-border-radius);
}

.tasks-view-header {
  display: flex;
  align-items: center;
  justify-content: space-around;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.tasks-view-actions {
  display: contents;
}

.desktop-create-btn {
  display: none;
}

.tasks-list {
  font-size: var(--ds-font-size-lg);
}

.tasks-list-header {
  display: flex;
  gap: 12px;
  justify-content: space-between;
  margin-right: 10px;
  width: 100%;
}

:deep(.p-accordioncontent-wrapper) {
  max-width: 100%;
  overflow-x: hidden;
}

.drop-indicator {
  height: 2px;
  margin: 0 0.5rem;
  background: transparent;
  border-radius: 1px;
  transition: background 0.15s ease;
}

.drop-indicator.active {
  background: var(--p-primary-color);
  height: 3px;
  margin: 0 0.25rem;
}

.drop-indicator-last {
  min-height: 4px;
}

.completed-list {
  display: flex;
  flex-direction: column;
}

.completed-list .task-card-wrapper {
  padding: 0 0.5rem;
}

.completed-list .task-card-wrapper.completed-loading {
  padding: 0.25rem 0.5rem;
}

.completed-load-more-sentinel {
  min-height: 1px;
  width: 100%;
}

.task-list-move {
  transition: transform 0.25s ease;
}

.task-list-enter-active {
  transition: opacity 0.2s ease;
}

.task-list-enter-from {
  opacity: 0;
}

.calendar-view {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding: 0 12px;
}

.calendar-view-top,
.calendar-view-bottom {
  flex: 1;
}

.calendar-picker {
  width: 100%;
}

.calendar-day {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 100%;
}

.calendar-day.has-tasks {
  position: relative;
  font-weight: 600;
}

.calendar-day.has-tasks::after {
  content: '';
  position: absolute;
  bottom: 0px;
  left: 50%;
  transform: translateX(-50%);
  width: 6px;
  height: 6px;
  border-radius: 999px;
  background: var(--p-primary-color);
}

.calendar-day-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 0.5rem;
  font-size: var(--ds-font-size-lg);
}

@media (min-width: 900px) {
  .tasks-view-root {
    max-width: 78rem;
    margin: 0 auto;
  }

  .tasks-view-header {
    justify-content: space-between;
    margin-bottom: 1rem;
  }

  .tasks-view-actions {
    display: flex;
    align-items: center;
    justify-content: flex-end;
    gap: 0.75rem;
    margin-left: auto;
  }

  .desktop-create-btn {
    display: inline-flex;
  }

  .tasks-view-root :deep(.empty-state) {
    margin-top: 3rem;
  }

  .tasks-skeleton {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(22rem, 1fr));
    gap: 1rem;
    padding: 0;
  }

  .tasks-list {
    border: 1px solid var(--p-content-border-color);
    border-radius: var(--ds-radius-lg);
    overflow: hidden;
    margin-bottom: 0.75rem;
    background: var(--p-card-background);
  }

  .calendar-view {
    display: grid;
    grid-template-columns: minmax(20rem, 26rem) minmax(24rem, 1fr);
    align-items: start;
    gap: 1.25rem;
    padding: 0;
  }

  .calendar-view-top,
  .calendar-view-bottom {
    min-width: 0;
  }

  .calendar-view-top {
    position: sticky;
    top: 0;
  }
}
</style>
