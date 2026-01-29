<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'

import Accordion from 'primevue/accordion'
import AccordionPanel from 'primevue/accordionpanel'
import AccordionHeader from 'primevue/accordionheader'
import AccordionContent from 'primevue/accordioncontent'
import Badge from 'primevue/badge'
import TaskCard from '@/components/TaskCard.vue'
import { getTasks, updateTask, type TaskDTO } from '@/api/TasksAPI'
import { useI18n } from 'vue-i18n'

const { t } = useI18n();
const emit = defineEmits<{
  (e: 'edit-task', task: TaskDTO): void
}>()

const tasks = ref<TaskDTO[]>([])

onMounted(async () => {
  tasks.value = await getTasks()
})

function isToday(date: Date) {
  const today = new Date()
  return date.toDateString() === today.toDateString()
}

function isThisWeek(date: Date) {
  const now = new Date()
  const start = new Date(now)
  start.setDate(now.getDate() - now.getDay())
  const end = new Date(start)
  end.setDate(start.getDate() + 7)
  return date >= start && date < end
}

const overdueTasks = computed(() =>
  tasks.value.filter(t => !t.completionDate && t.dueDate && new Date(t.dueDate) < new Date())
)
const todayTasks = computed(() =>
  tasks.value.filter(t => !t.completionDate && t.dueDate && isToday(new Date(t.dueDate)) && !overdueTasks.value.includes(t))
)
const weekTasks = computed(() =>
  tasks.value.filter(t => !t.completionDate && t.dueDate && isThisWeek(new Date(t.dueDate)) && !overdueTasks.value.includes(t) && !todayTasks.value.includes(t))
)
const completedTasks = computed(() =>
  tasks.value.filter(t => t.completionDate)
)
const inboxTasks = computed(() =>
  tasks.value.filter(t => !overdueTasks.value.includes(t) && !todayTasks.value.includes(t) && !weekTasks.value.includes(t) && !completedTasks.value.includes(t))
)

const taskSections = computed(() => [
  { title: t('tasks.list.overdue'), tasks: overdueTasks.value },
  { title: t('tasks.list.today'), tasks: todayTasks.value },
  { title: t('tasks.list.thisweek'), tasks: weekTasks.value },
  { title: t('tasks.list.inbox'), tasks: inboxTasks.value },
  { title: t('tasks.list.completed'), tasks: completedTasks.value },
])


function onEditTask(task: TaskDTO) {
  emit('edit-task', task)
}

function onTaskCompletionChange(taskId: string, completed: boolean) {
  const task = tasks.value.find(t => t.id === taskId)
  if (!task) return

  task.completionDate = completed ? new Date().toISOString() : null
  handleCompletionSideEffects(task, completed)
}

async function handleCompletionSideEffects(task: TaskDTO, completed: boolean) {
  const previousCompletionDate = task.completionDate
  await delay(400)

  try {
    await updateTask(task.id, {
      title: task.title,
      description: task.description,
      dueDate: task.dueDate,
      completionDate: completed ? new Date().toISOString() : null,
      goalId: task.goalId
    })
  } catch (e) {
    task.completionDate = previousCompletionDate ? null : previousCompletionDate
  }
}

function delay(ms: number) {
  return new Promise(resolve => setTimeout(resolve, ms))
}

</script>

<template>
  <Accordion :value="['0']" multiple>
    <template v-for="(section, index) in taskSections" :key="section.title">
      <AccordionPanel v-if="section.tasks.length" :value="String(index)" class="tasks-list">
        <AccordionHeader>
          <div class="tasks-list-header">
            <span>{{ section.title }}</span>
            <Badge>{{ section.tasks.length }}</Badge>
          </div>
        </AccordionHeader>

        <AccordionContent>
          <TransitionGroup name="task-list">
            <TaskCard v-for="task in section.tasks" :key="task.id" :task="task"
              @completion-change="onTaskCompletionChange" @edit="onEditTask" />
          </TransitionGroup>
        </AccordionContent>
      </AccordionPanel>
    </template>
  </Accordion>
</template>


<style scoped>
.tasks-list {
  margin: 12px;
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