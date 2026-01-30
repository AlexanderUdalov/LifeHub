<script setup lang="ts">
import Card from 'primevue/card'
import Checkbox from 'primevue/checkbox'
import { computed, ref, watch } from 'vue'
import type { TaskDTO } from '@/api/TasksAPI';
import { useDeadlineFormatter } from '@/composables/useDeadlineFormatter'

const props = defineProps<{ task: TaskDTO }>()
const emit = defineEmits<{
  (e: 'edit', task: TaskDTO): void,
  (e: 'completion-change', taskId: string, value: boolean): void
}>()

const localCompleted = ref<boolean>(!!props.task.completionDate)
watch(localCompleted, value => {
  emit('completion-change', props.task.id, value)
})

const isExpanded = ref(false)
const LONG_PRESS_MS = 500

let pressTimer: number | null = null
let longPressTriggered = false

function onPressStart() {
  longPressTriggered = false
  pressTimer = window.setTimeout(() => {
    longPressTriggered = true
    emit('edit', props.task)
  }, LONG_PRESS_MS)
}

function onPressEnd() {
  if (pressTimer) {
    clearTimeout(pressTimer)
    pressTimer = null
  }

  if (!longPressTriggered) {
    isExpanded.value = !isExpanded.value
  }
}

const isOverdue = computed(() => {
  if (!props.task.dueDate || localCompleted.value) return false

  const today = new Date()
  today.setHours(0, 0, 0, 0)

  const due = new Date(props.task.dueDate)
  due.setHours(0, 0, 0, 0)

  return due < today
})

const { formatDeadline } = useDeadlineFormatter()
const deadlineText = computed(() => {
  if (!props.task.dueDate) return null
  return formatDeadline(new Date(props.task.dueDate))
})
</script>


<template>
  <Card class="task-card" @pointerdown="onPressStart" @pointerup="onPressEnd">
    <template #title>
      <Checkbox v-model="localCompleted" name="completed" binary />
      <label for="completed" class="title" :class="{ completed: localCompleted }"> {{ task.title }} </label>
    </template>
    <template #content>
      <p v-if="props.task.description" class="description" :class="{ completed: localCompleted, expanded: isExpanded }">
        {{ props.task.description }}
      </p>

      <p v-if="deadlineText" class="deadline" :class="{ overdue: isOverdue, completed: localCompleted }">
        {{ deadlineText }}
      </p>
    </template>
  </Card>
</template>

<style scoped>
.task-card {
  border-radius: 0px;
}

:deep(.p-card-body) {
  padding: 1rem;
}

.title {
  vertical-align: middle;
  padding-left: 1rem;
  font-size: 1.25rem;
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

.description.expanded {
  white-space: normal;
  overflow: visible;
  text-overflow: unset;
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
