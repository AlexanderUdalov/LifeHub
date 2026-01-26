<script setup lang="ts">
import Card from 'primevue/card'
import Checkbox from 'primevue/checkbox'
import { computed, ref, watch } from 'vue'
import type { TaskDTO } from '@/api/TasksAPI';

const props = defineProps<{ task: TaskDTO }>()
const emit = defineEmits<{
  (e: 'edit', task: TaskDTO): void
}>()

const localCompleted = ref<boolean>(!!props.task.completionDate)

watch(localCompleted, value => {
  props.task.completionDate = value ? new Date().toISOString() : null
})

const isOverdue = computed(() => {
  if (!props.task.dueDate || localCompleted.value) return false

  const today = startOfDay(new Date())
  const due = startOfDay(new Date(props.task.dueDate))

  return due < today;
})

function startOfDay(date: Date) {
  const d = new Date(date)
  d.setHours(0, 0, 0, 0)
  return d
}

function diffInDays(from: Date, to: Date) {
  const msPerDay = 24 * 60 * 60 * 1000
  return Math.round((startOfDay(to).getTime() - startOfDay(from).getTime()) / msPerDay)
}

function getDaysDiffString(target: Date): string {
  const now = new Date()
  const due = new Date(target)

  const daysDiff = diffInDays(now, due)
  if (daysDiff === 0) return 'Сегодня'
  if (daysDiff === 1) return 'Завтра'
  if (daysDiff === -1) return 'Вчера'

  if (daysDiff > 1 && daysDiff <= 7) {
    return `Через ${daysDiff} дн`
  }

  if (daysDiff < -1 && daysDiff >= -7) {
    return `${Math.abs(daysDiff)} дн назад`
  }

  // Если дальше недели — можно день недели
  if (daysDiff > 0 && daysDiff <= 14) {
    return `В следующий ${due.toLocaleDateString('ru-RU', { weekday: 'long' })}`
  }

  // Фолбэк — обычная дата
  return due.toLocaleString('ru-RU')
}

function getTimeString(date: Date) {
  const hours = date.getHours()
  const minutes = date.getMinutes()

  // Если время ровно 00:00 — считаем, что его нет
  if (hours === 0 && minutes === 0) return null

  return date.toLocaleTimeString('ru-RU', {
    hour: '2-digit',
    minute: '2-digit',
  })
}

const deadlineText = computed(() => {
  if (!props.task.dueDate) return null

  const due = new Date(props.task.dueDate)
  const dayText = getDaysDiffString(due)
  const timeText = getTimeString(due)

  return timeText ? `${dayText}, ${timeText}` : dayText;
})
</script>


<template>
  <Card class="task-card">
    <template #title>
      <Checkbox v-model="localCompleted" name="completed" binary />
      <label for="completed" class="title" :class="{ completed: localCompleted }"> {{ task.title }} </label>
    </template>
    <template #content>
      <p v-if="props.task.description" class="description" :class="{ completed: localCompleted }">
        {{ props.task.description }}
      </p>

      <p v-if="deadlineText" class="deadline" :class="{ overdue: isOverdue, completed: localCompleted }">
        {{ deadlineText }}
      </p>
    </template>
  </Card>
</template>

<style scoped>
:deep(.p-card-body) {
  padding: 1rem;
}

.title {
  vertical-align: middle;
  padding-left: 1rem;
}

.title.completed {
  text-decoration: line-through;
  color: var(--p-gray-400);
}

.description {
  color: var(--p-gray-400);
  font-size: medium;
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
  margin-top: 0.5rem;
  margin-bottom: 0.5rem;
}

.description.completed {
  color: var(--p-gray-500);
}

.deadline {
  font-size: small;
  color: var(--p-orange-400);
  margin: 0;
}

.deadline.overdue {
  color: var(--p-red-600);
}

.deadline.completed {
  color: var(--p-gray-400);
}
</style>
