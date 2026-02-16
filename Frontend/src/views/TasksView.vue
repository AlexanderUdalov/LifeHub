<script setup lang="ts">
import { onMounted, computed, ref } from 'vue'

import Accordion from 'primevue/accordion'
import AccordionPanel from 'primevue/accordionpanel'
import AccordionHeader from 'primevue/accordionheader'
import AccordionContent from 'primevue/accordioncontent'
import Badge from 'primevue/badge'
import TaskCard from '@/components/TaskCard.vue'
import { type TaskDTO } from '@/api/TasksAPI'
import { useI18n } from 'vue-i18n'
import { useTasksStore } from '@/stores/tasks'

const { t } = useI18n()
const emit = defineEmits<{
  (e: 'edit-task', task: TaskDTO): void
}>()

const tasksStore = useTasksStore()

onMounted(() => {
  tasksStore.fetchTasks()
})

const taskSections = computed(() => [
  { key: 'overdue', title: t('tasks.list.overdue'), tasks: tasksStore.overdueTasks, draggable: false },
  { key: 'today', title: t('tasks.list.today'), tasks: tasksStore.todayTasks, draggable: true },
  { key: 'thisweek', title: t('tasks.list.thisweek'), tasks: tasksStore.weekTasks, draggable: false },
  { key: 'inbox', title: t('tasks.list.inbox'), tasks: tasksStore.inboxTasks, draggable: true },
  { key: 'completed', title: t('tasks.list.completed'), tasks: tasksStore.completedTasks, draggable: false },
])

function onEditTask(task: TaskDTO) {
  emit('edit-task', task)
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
      if (clientY < rect.top) return i
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
  }

  function onPointerUp(e: PointerEvent) {
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
    tasksStore.reorderTasksInList(newOrder)
    dropSectionKey.value = null
  }

  const fromIndex = taskIndex
  document.body.style.cursor = 'grabbing'
  document.addEventListener('pointermove', onPointerMove)
  document.addEventListener('pointerup', onPointerUp)
  document.addEventListener('pointercancel', cleanup)
}
</script>

<template>
  <Accordion :value="['0']" multiple>
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
</template>


<style scoped>
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
</style>
