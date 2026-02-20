<script setup lang="ts">
import Card from 'primevue/card'
import Checkbox from 'primevue/checkbox'
import { computed, ref, watch } from 'vue'
import type { TaskDTO } from '@/api/TasksAPI'
import { useDeadlineFormatter } from '@/composables/useDeadlineFormatter'
import { useRecurrenceFormatter } from '@/composables/useRecurrenceFormatter'

const props = withDefaults(
  defineProps<{ task: TaskDTO; draggable?: boolean }>(),
  { draggable: false }
)
const emit = defineEmits<{
  (e: 'edit', task: TaskDTO): void
  (e: 'completion-change', taskId: string, value: boolean): void
  (e: 'drag-start', event: PointerEvent): void
}>()

const localCompleted = ref<boolean>(!!props.task.completionDate)
watch(localCompleted, value => {
  emit('completion-change', props.task.id, value)
})

const isExpanded = ref(false)

function onHeaderClick() {
  emit('edit', props.task)
}

function onContentClick() {
  isExpanded.value = !isExpanded.value
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

const { formatRecurrence } = useRecurrenceFormatter()
const recurrenceText = computed(() =>
  formatRecurrence(props.task.recurrenceRule, props.task.dueDate)
)
</script>


<template>
  <Card class="task-card">
    <template #title>
      <div class="task-card-title-row" @click="onHeaderClick">
        <Checkbox v-model="localCompleted" name="completed" binary @click.stop />
        <label for="completed" class="title title-clickable" :class="{ completed: localCompleted }">
          {{ task.title }}
        </label>
        <div v-if="draggable" class="drag-handle" @pointerdown.prevent.stop="emit('drag-start', $event)">
          <i class="pi pi-bars" />
        </div>
      </div>
    </template>
    <template #content>
      <p v-if="props.task.description" class="description" :class="{ completed: localCompleted, expanded: isExpanded }"
        @click="onContentClick">
        {{ props.task.description }}
      </p>

      <div v-if="deadlineText || recurrenceText" class="deadline-row">
        <span v-if="deadlineText" class="deadline" :class="{ overdue: isOverdue, completed: localCompleted }">
          {{ deadlineText }}
        </span>
        <span v-if="recurrenceText" class="recurrence" :class="{ completed: localCompleted }">
          <i class="pi pi-refresh recurrence-icon" aria-hidden="true"></i>
          {{ recurrenceText }}
        </span>
      </div>
    </template>
  </Card>
</template>

<style scoped>
.task-card {
  border-radius: 0px;
  user-select: none;
  -webkit-user-select: none;
  -webkit-touch-callout: none;
}

:deep(.p-card-body) {
  padding: 1rem;
}

.task-card-title-row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  width: 100%;
  min-width: 0;
  cursor: pointer;
}

.title {
  flex: 1;
  min-width: 0;
  vertical-align: middle;
  padding-left: 1rem;
  font-size: 1.25rem;
}

.title-clickable {
  cursor: pointer;
}

.drag-handle {
  flex-shrink: 0;
  margin-left: 0.25rem;
  padding: 0.25rem;
  color: var(--p-text-muted-color);
  cursor: grab;
  touch-action: none;
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

.deadline-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.75rem;
  font-size: small;
  margin: 0;
}

.deadline {
  color: var(--p-orange-400);
}

.deadline.overdue {
  color: var(--p-red-600);
}

.deadline.completed {
  color: var(--p-gray-400);
}

.recurrence {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  color: var(--p-gray-500);
}

.recurrence-icon {
  font-size: 0.875em;
}

.recurrence.completed {
  color: var(--p-gray-400);
}
</style>
