<script setup lang="ts">
defineOptions({ name: 'TasksView' })
import { onMounted, computed, ref } from 'vue'

import Accordion from 'primevue/accordion'
import AccordionPanel from 'primevue/accordionpanel'
import AccordionHeader from 'primevue/accordionheader'
import AccordionContent from 'primevue/accordioncontent'
import Badge from 'primevue/badge'
import DatePicker from 'primevue/datepicker'
import Skeleton from 'primevue/skeleton'
import EmptyState from '@/components/EmptyState.vue'
import TaskCard from '@/components/TaskCard.vue'
import { type TaskDTO } from '@/api/TasksAPI'
import { useI18n } from 'vue-i18n'
import { useTasksStore } from '@/stores/tasks'
import { getStoredTaskViewMode, setStoredTaskViewMode, type TaskViewMode } from '@/utils/taskViewMode'
import SelectButton from 'primevue/selectbutton'
import { isToday, isSameDateOnly, startOfDay, toDateOnlyString } from '@/utils/dateOnly'

const { t } = useI18n()
const emit = defineEmits<{
  (e: 'edit-task', task: TaskDTO): void
}>()

const tasksStore = useTasksStore()

onMounted(() => {
  tasksStore.fetchTasks()
})

const taskViewMode = ref<TaskViewMode>(getStoredTaskViewMode() ?? 'standard')

const taskViewModeOptions = computed(() => [
  { label: t('profile-view.task-list-view-standard'), value: 'standard' as TaskViewMode, icon: 'pi pi-bars' },
  { label: t('profile-view.task-list-view-compact'), value: 'compact' as TaskViewMode, icon: 'pi pi-align-justify' },
  { label: t('profile-view.task-list-view-calendar'), value: 'calendar' as TaskViewMode, icon: 'pi pi-calendar' },
])

function onTaskViewModeChange(mode: TaskViewMode) {
  setStoredTaskViewMode(mode)
  taskViewMode.value = mode
}

const taskSections = computed(() => [
  { key: 'overdue', title: t('tasks.list.overdue'), tasks: tasksStore.overdueTasks, draggable: false },
  { key: 'today', title: t('tasks.list.today'), tasks: tasksStore.todayTasks, draggable: true },
  { key: 'thisweek', title: t('tasks.list.thisweek'), tasks: tasksStore.weekTasks, draggable: true },
  { key: 'inbox', title: t('tasks.list.inbox'), tasks: tasksStore.inboxTasks, draggable: true },
  { key: 'completed', title: t('tasks.list.completed'), tasks: tasksStore.completedTasks, draggable: false },
])

const hasAnyTasks = computed(() => tasksStore.tasks.length > 0)

function onEditTask(task: TaskDTO) {
  emit('edit-task', task)
}

const selectedDate = ref<Date>(startOfDay(new Date()))

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
  const includeUndated = isToday(selected)

  const list = tasksStore.tasks.filter(t => {
    if (t.completionDate) return false
    if (!t.dueDate) return includeUndated
    return isSameDateOnly(new Date(t.dueDate), selected)
  })

  return list.sort((a, b) => {
    const aHasDate = a.dueDate != null
    const bHasDate = b.dueDate != null
    if (aHasDate !== bHasDate) return aHasDate ? -1 : 1
    const ao = a.sortOrder as number | null | undefined
    const bo = b.sortOrder as number | null | undefined
    if (ao == null && bo == null) return 0
    if (ao == null) return 1
    if (bo == null) return -1
    return ao - bo
  })
})

function calendarCellKey(date: { year: number; month: number; day: number }): string {
  return `${date.year}-${String(date.month + 1).padStart(2, '0')}-${String(date.day).padStart(2, '0')}`
}

const dropSectionKey = ref<string | null>(null)
const dropIndex = ref(0)

function onDragStart(sectionKey: string, taskIndex: number, _event: PointerEvent) {
  if (!(_event.target instanceof HTMLElement)) return

  const container = _event.target.closest('[data-draggable-list]')
  if (!container) return

  const section = taskSections.value.find(s => s.key === sectionKey)
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
          sortOrder: removed.sortOrder
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
      <h1 class="view-page-header">{{ $t('tasks.tasks') }}</h1>
      <SelectButton v-model="taskViewMode" :options="taskViewModeOptions" option-label="label" option-value="value"
        :aria-label="$t('profile-view.task-list-view')" @change="onTaskViewModeChange(taskViewMode)">
        <template #option="slotProps">
          <i :class="slotProps.option.icon" :aria-hidden="true" />
        </template>
      </SelectButton>
    </header>

    <div v-if="tasksStore.isLoading && tasksStore.tasks.length === 0" class="tasks-skeleton">
      <Skeleton v-for="i in 4" :key="i" height="3.5rem" class="skeleton-row" />
    </div>

    <EmptyState v-else-if="!hasAnyTasks" icon="pi pi-list-check" :title="$t('tasks.empty')"
      :subtitle="$t('tasks.emptySubtitle')" />

    <Accordion v-else-if="taskViewMode === 'standard'" :value="['0']" multiple>
      <template v-for="(section, index) in taskSections" :key="section.key">
        <AccordionPanel v-if="section.tasks.length" :value="String(index)" class="tasks-list">
          <AccordionHeader>
            <div class="tasks-list-header">
              <span>{{ section.title }}</span>
              <Badge>{{ section.tasks.length }}</Badge>
            </div>
          </AccordionHeader>

          <AccordionContent>
            <TransitionGroup name="task-list">
              <template v-if="section.draggable">
                <div data-draggable-list>
                  <div v-for="(task, taskIndex) in section.tasks" :key="task.id" data-task-row>
                    <div class="drop-indicator"
                      :class="{ active: dropSectionKey === section.key && dropIndex === taskIndex }" />
                    <TaskCard class="task-card" :task="task" :draggable="true"
                      @completion-change="tasksStore.toggleTaskCompletion" @edit="onEditTask"
                      @drag-start="(e) => onDragStart(section.key, taskIndex, e)" />
                  </div>
                  <div class="drop-indicator drop-indicator-last"
                    :class="{ active: dropSectionKey === section.key && dropIndex === section.tasks.length }" />
                </div>
              </template>
              <template v-else>
                <TaskCard v-for="task in section.tasks" :key="task.id" :task="task"
                  @completion-change="tasksStore.toggleTaskCompletion" @edit="onEditTask" />
              </template>
            </TransitionGroup>
          </AccordionContent>
        </AccordionPanel>
      </template>
    </Accordion>

    <Accordion v-else-if="taskViewMode === 'compact'" :value="['0']" multiple>
      <template v-for="(section, index) in taskSections" :key="section.key">
        <AccordionPanel v-if="section.tasks.length" :value="String(index)" class="tasks-list">
          <AccordionHeader>
            <div class="tasks-list-header">
              <span>{{ section.title }}</span>
              <Badge>{{ section.tasks.length }}</Badge>
            </div>
          </AccordionHeader>

          <AccordionContent>
            <TransitionGroup name="task-list">
              <template v-if="section.draggable">
                <div data-draggable-list>
                  <div v-for="(task, taskIndex) in section.tasks" :key="task.id" data-task-row>
                    <div class="drop-indicator"
                      :class="{ active: dropSectionKey === section.key && dropIndex === taskIndex }" />
                    <TaskCard class="task-card" :task="task" :draggable="true" :compact="true"
                      :hide-deadline="section.key === 'today'" :hide-goal="true"
                      @completion-change="tasksStore.toggleTaskCompletion" @edit="onEditTask"
                      @drag-start="(e) => onDragStart(section.key, taskIndex, e)" />
                  </div>
                  <div class="drop-indicator drop-indicator-last"
                    :class="{ active: dropSectionKey === section.key && dropIndex === section.tasks.length }" />
                </div>
              </template>
              <template v-else>
                <TaskCard v-for="task in section.tasks" :key="task.id" :task="task" :compact="true"
                  :hide-deadline="section.key === 'today'" :hide-goal="true"
                  @completion-change="tasksStore.toggleTaskCompletion" @edit="onEditTask" />
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
            <span class="calendar-day" :class="{ 'has-tasks': calendarTaskDateKeys.has(calendarCellKey(date)) }">
              {{ date.day }}
            </span>
          </template>
        </DatePicker>
      </div>

      <div class="calendar-view-bottom">
        <div class="calendar-day-header">
          <span>{{ selectedDate.toLocaleDateString() }}</span>
          <Badge>{{ calendarTasksForSelectedDay.length }}</Badge>
        </div>

        <TransitionGroup name="task-list">
          <TaskCard v-for="task in calendarTasksForSelectedDay" :key="task.id" :task="task"
            @completion-change="tasksStore.toggleTaskCompletion" @edit="onEditTask" />
        </TransitionGroup>
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

.view-page-header {
  font-size: var(--p-card-title-font-size);
  font-weight: 600;
  text-align: center;
}

.tasks-list {
  font-size: large;
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
  font-size: large;
}
</style>
