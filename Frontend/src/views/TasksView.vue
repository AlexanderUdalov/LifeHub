<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'
import { tasksApi } from '@/api/TasksAPI'
import type { TaskItem } from '@/models/TaskItem'

import Accordion from 'primevue/accordion'
import AccordionPanel from 'primevue/accordionpanel'
import AccordionHeader from 'primevue/accordionheader'
import AccordionContent from 'primevue/accordioncontent'
import Badge from 'primevue/badge'
import TaskCard from '@/components/TaskCard.vue'

const emit = defineEmits<{
  (e: 'edit-task', task: TaskItem): void
}>()

const tasks = ref<TaskItem[]>([])

onMounted(async () => {
  tasks.value = await tasksApi.getTasks()
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
  tasks.value.filter(t => !t.completeDate && t.dueDate && new Date(t.dueDate) < new Date())
)

const todayTasks = computed(() =>
  tasks.value.filter(t => !t.completeDate && t.dueDate && isToday(new Date(t.dueDate)))
)

const weekTasks = computed(() =>
  tasks.value.filter(t => !t.completeDate && t.dueDate && isThisWeek(new Date(t.dueDate)))
)

const completedTasks = computed(() =>
  tasks.value.filter(t => t.completeDate && isToday(new Date(t.completeDate)))
)

function onEditTask(task: TaskItem) {
  emit('edit-task', task)
}

</script>

<template>
  <Accordion value="0">
    <AccordionPanel value="0" class="tasks-list" v-if="overdueTasks.length">
      <AccordionHeader>
        <div class="tasks-list-header">
          <span>Просрочено</span>
          <Badge>{{ overdueTasks.length }} </Badge>
        </div>
      </AccordionHeader>
      <AccordionContent>
        <TaskCard v-for="task in overdueTasks" :key="task.id" :task="task" @edit="onEditTask" />
      </AccordionContent>
    </AccordionPanel>

    <AccordionPanel value="1" class="tasks-list" v-if="todayTasks.length">
      <AccordionHeader>Сегодня</AccordionHeader>
      <AccordionContent>
        <TaskCard v-for="task in todayTasks" :key="task.id" :task="task" @edit="onEditTask" />
      </AccordionContent>
    </AccordionPanel>

    <AccordionPanel value="2" class="tasks-list" v-if="weekTasks.length">
      <AccordionHeader>На этой неделе</AccordionHeader>
      <AccordionContent>
        <TaskCard v-for="task in weekTasks" :key="task.id" :task="task" @edit="onEditTask" />
      </AccordionContent>
    </AccordionPanel>

    <AccordionPanel value="3" class="tasks-list" v-if="completedTasks.length">
      <AccordionHeader>Выполнено</AccordionHeader>
      <AccordionContent>
        <TaskCard v-for="task in completedTasks" :key="task.id" :task="task" @edit="onEditTask" />
      </AccordionContent>
    </AccordionPanel>
  </Accordion>
</template>

<style scoped>
.tasks-list {
  margin: 12px;
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
</style>