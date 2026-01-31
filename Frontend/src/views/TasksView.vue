<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'

import Accordion from 'primevue/accordion'
import AccordionPanel from 'primevue/accordionpanel'
import AccordionHeader from 'primevue/accordionheader'
import AccordionContent from 'primevue/accordioncontent'
import Badge from 'primevue/badge'
import TaskCard from '@/components/TaskCard.vue'
import { type TaskDTO } from '@/api/TasksAPI'
import { useI18n } from 'vue-i18n'
import { useTasksStore } from '@/stores/tasks'

const { t } = useI18n();
const emit = defineEmits<{
  (e: 'edit-task', task: TaskDTO): void
}>()

const tasksStore = useTasksStore()

onMounted(() => {
  tasksStore.fetchTasks()
})

const taskSections = computed(() => [
  { title: t('tasks.list.overdue'), tasks: tasksStore.overdueTasks },
  { title: t('tasks.list.today'), tasks: tasksStore.todayTasks },
  { title: t('tasks.list.thisweek'), tasks: tasksStore.weekTasks },
  { title: t('tasks.list.inbox'), tasks: tasksStore.inboxTasks },
  { title: t('tasks.list.completed'), tasks: tasksStore.completedTasks },
])

function onEditTask(task: TaskDTO) {
  emit('edit-task', task)
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
              @completion-change="tasksStore.toggleTaskCompletion" @edit="onEditTask" />
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