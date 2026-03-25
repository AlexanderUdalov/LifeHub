<script setup lang="ts">
import Card from 'primevue/card'
import Checkbox from 'primevue/checkbox'
import { computed, ref, watch } from 'vue'
import type { TaskDTO } from '@/api/TasksAPI'
import { useDeadlineFormatter } from '@/composables/useDeadlineFormatter'
import { useRecurrenceFormatter } from '@/composables/useRecurrenceFormatter'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { useGoalsStore } from '@/stores/goals'

const props = withDefaults(
  defineProps<{
    task: TaskDTO
    draggable?: boolean
    noBorder?: boolean
    compact?: boolean
    hideDeadline?: boolean
    hideGoal?: boolean
  }>(),
  { draggable: false, noBorder: false, compact: false, hideDeadline: false, hideGoal: false }
)
const lifeAreasStore = useLifeAreasStore()
const goalsStore = useGoalsStore()
const areaColor = computed(() => lifeAreasStore.getAreaColorById(props.task.lifeAreaId))
const cardBorderStyle = computed(() => {
  if (props.noBorder) return { border: 'none', borderLeftWidth: 0 }
  return areaColor.value ? { borderLeftWidth: '4px', borderLeftStyle: 'solid', borderLeftColor: areaColor.value } : { borderLeftWidth: 0 }
})

type DescriptionPart =
  | { type: 'text'; value: string }
  | { type: 'link'; value: string }

const descriptionParts = computed<DescriptionPart[]>(() => {
  const source = props.task.description ?? ''
  if (!source) return []

  const urlRegex = /(https?:\/\/[^\s]+)/g
  const parts: DescriptionPart[] = []
  let lastIndex = 0
  let match: RegExpExecArray | null

  while ((match = urlRegex.exec(source)) !== null) {
    const [url] = match
    const start = match.index

    if (start > lastIndex) {
      parts.push({ type: 'text', value: source.slice(lastIndex, start) })
    }

    parts.push({ type: 'link', value: url })
    lastIndex = start + url.length
  }

  if (lastIndex < source.length) {
    parts.push({ type: 'text', value: source.slice(lastIndex) })
  }

  return parts
})

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
const goalTitle = computed(() => goalsStore.getGoalById(props.task.goalId)?.title ?? null)
const hasDescription = computed(() => !!props.task.description?.trim())
const showCompactDeadline = computed(
  // In compact mode show due date for active tasks (so "this week" can display a date).
  // Overdue color is still determined by `isOverdue` for the deadline styling.
  () => props.compact && !props.hideDeadline && !!deadlineText.value && !localCompleted.value
)
const showRegularDeadline = computed(
  () => !props.compact && !props.hideDeadline && !!deadlineText.value
)
const showRecurrence = computed(() => !props.compact && !!recurrenceText.value)
const showGoal = computed(() => !props.compact && !props.hideGoal && !!goalTitle.value)
const showDeadlineRow = computed(
  () =>
    showCompactDeadline.value ||
    showRegularDeadline.value ||
    showRecurrence.value ||
    showGoal.value
)
</script>


<template>
  <Card class="task-card" :class="{ 'no-border': noBorder, compact: props.compact }" :style="cardBorderStyle">
    <template #title>
      <div class="task-card-title-row" @click="onHeaderClick">
        <Checkbox v-model="localCompleted" name="completed" binary @click.stop />
        <label for="completed" class="title title-clickable" :class="{ completed: localCompleted }">
          {{ props.compact && hasDescription ? `${task.title}…` : task.title }}
        </label>
        <div v-if="draggable" class="drag-handle" @pointerdown.prevent.stop="emit('drag-start', $event)">
          <i class="pi pi-bars" />
        </div>
      </div>
    </template>
    <template #content>
      <p v-if="!props.compact && props.task.description" class="description"
        :class="{ completed: localCompleted, expanded: isExpanded }" @click="onContentClick">
        <template v-if="!isExpanded">
          {{ props.task.description }}
        </template>
        <template v-else>
          <template v-for="(part, index) in descriptionParts" :key="index">
            <a v-if="part.type === 'link'" :href="part.value" target="_blank" rel="noopener noreferrer"
              class="description-link" @click.stop>
              {{ part.value }}
            </a>
            <span v-else>{{ part.value }}</span>
          </template>
        </template>
      </p>

      <div v-if="showDeadlineRow" class="deadline-row">
        <span class="deadline-row-left">
          <span v-if="showCompactDeadline || showRegularDeadline" class="deadline"
            :class="{ overdue: isOverdue, completed: localCompleted }">
            {{ deadlineText }}
          </span>
          <span v-if="showRecurrence" class="recurrence" :class="{ completed: localCompleted }">
            <i class="pi pi-refresh recurrence-icon" aria-hidden="true"></i>
            {{ recurrenceText }}
          </span>
        </span>
        <span v-if="showGoal" class="goal-text">
          {{ goalTitle }}
        </span>
      </div>
    </template>
  </Card>
</template>

<style scoped>
.task-card {
  border-radius: 0px;
  border-left-width: 4px;
  border-left-style: solid;
  user-select: none;
  -webkit-user-select: none;
  -webkit-touch-callout: none;
}

.task-card.no-border {
  border: none;
  border-left-width: 0;
}

.task-card.no-border :deep(.p-card) {
  border: none;
  box-shadow: none;
}

:deep(.p-card-body) {
  padding: 1rem;
}

.task-card.compact :deep(.p-card-body) {
  padding: 0.5rem 0.75rem;
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

.task-card.compact .title {
  font-size: 1rem;
  padding-left: 0.75rem;
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
  white-space: pre-line;
  overflow: visible;
  text-overflow: unset;
}

.description-link {
  color: var(--p-primary-color);
  text-decoration: underline;
  cursor: pointer;
  word-break: break-all;
}

.deadline-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.75rem;
  font-size: small;
  margin: 0;
}

.deadline-row-left {
  display: inline-flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.75rem;
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

.goal-text {
  color: var(--p-text-muted-color);
  margin-left: auto;
}
</style>
