<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { TaskDTO } from '@/api/TasksAPI'
import { useDeadlineFormatter } from '@/composables/useDeadlineFormatter'
import { useRecurrenceFormatter } from '@/composables/useRecurrenceFormatter'
import { useClampedContent } from '@/composables/useClampedContent'
import { renderMarkdown } from '@/composables/useMarkdown'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { useGoalsStore } from '@/stores/goals'
import { toDateOnlyString } from '@/utils/dateOnly'
const props = withDefaults(
  defineProps<{
    task: TaskDTO
    draggable?: boolean
    noBorder?: boolean
    compact?: boolean
    hideDeadline?: boolean
    hideGoal?: boolean
  }>(),
  { draggable: false, noBorder: false, compact: false, hideDeadline: false, hideGoal: false },
)
const emit = defineEmits<{
  (e: 'edit', task: TaskDTO): void
  (e: 'completion-change', id: string, value: boolean): void
  (e: 'drag-start', event: PointerEvent): void
}>()
const { t } = useI18n()
const lifeAreas = useLifeAreasStore(),
  goals = useGoalsStore()
const area = computed(() => lifeAreas.lifeAreas.find((x) => x.id === props.task.lifeAreaId))
const goal = computed(() => goals.getGoalById(props.task.goalId))
const { formatDeadline } = useDeadlineFormatter(),
  { formatRecurrence } = useRecurrenceFormatter()
const deadline = computed(() =>
  props.task.dueDate ? formatDeadline(new Date(props.task.dueDate)) : '',
)
const recurrence = computed(() => formatRecurrence(props.task.recurrenceRule, props.task.dueDate))
const overdue = computed(
  () =>
    !!props.task.dueDate &&
    !props.task.completionDate &&
    props.task.dueDate.slice(0, 10) < toDateOnlyString(new Date()),
)
const description = computed(() => renderMarkdown(props.task.description ?? ''))
const textRef = ref<HTMLElement | null>(null)
const { expanded, foldable, toggle } = useClampedContent(
  textRef,
  () => props.task.description ?? '',
  3,
)
</script>
<template>
  <article class="task-card" :class="{ compact, completed: !!task.completionDate }">
    <label class="task-check"
      ><input
        type="checkbox"
        :checked="!!task.completionDate"
        :aria-label="t('redesign.completeTask', { title: task.title })"
        @change="emit('completion-change', task.id, ($event.target as HTMLInputElement).checked)"
    /></label>
    <div class="task-content">
      <button type="button" class="task-title" @click="emit('edit', task)">{{ task.title }}</button
      ><template v-if="!compact && task.description"
        ><div class="task-description" :class="{ folded: foldable && !expanded }" @click="toggle">
          <div ref="textRef" class="task-description-inner" v-html="description" />
        </div>
        <button
          v-if="foldable"
          type="button"
          class="text-action task-fold"
          :aria-expanded="expanded"
          @click="toggle()"
        >
          {{ t(expanded ? 'redesign.collapse' : 'redesign.expand')
          }}<i
            aria-hidden="true"
            :class="expanded ? 'pi pi-angle-up' : 'pi pi-angle-down'"
          /></button
      ></template>
      <div class="task-meta">
        <span v-if="area" class="area-chip" :style="{ '--area-color': area.color }"
          ><span />{{ area.name }}</span
        ><span v-if="deadline && !hideDeadline" :class="{ overdue }"
          ><i aria-hidden="true" class="pi pi-calendar" />{{ deadline }}</span
        ><span v-if="recurrence"
          ><i aria-hidden="true" class="pi pi-refresh" />{{ recurrence }}</span
        ><RouterLink v-if="goal && !hideGoal" to="/goals"
          ><i aria-hidden="true" class="pi pi-flag" />{{ goal.title }}</RouterLink
        >
      </div>
    </div>
    <button
      v-if="draggable"
      type="button"
      class="drag-handle"
      :aria-label="t('redesign.reorderTask')"
      @pointerdown.prevent.stop="emit('drag-start', $event)"
    >
      <i aria-hidden="true" class="pi pi-bars" />
    </button>
  </article>
</template>
<style scoped>
.task-card {
  display: flex;
  align-items: stretch;
  gap: 9px;
  padding: 14px 0;
  border-bottom: 1px solid var(--app-line);
  background: transparent;
  min-width: 0;
  border-radius: 0;
}
.task-card:last-child {
  border-bottom: 0;
}
.task-check {
  display: flex;
  align-items: center;
  justify-content: center;
  min-width: 36px;
  align-self: stretch;
  cursor: pointer;
}
.task-check input {
  appearance: none;
  width: 22px;
  height: 22px;
  margin: 0;
  border: 1px solid var(--app-muted);
  border-radius: 6px;
  background: var(--app-card);
  display: grid;
  place-items: center;
  cursor: pointer;
}
.task-check input:checked {
  background: var(--app-accent);
  border-color: var(--app-accent);
}
.task-check input:checked:after {
  content: '✓';
  color: var(--app-on);
  font-size: 15px;
}
.task-content {
  flex: 1;
  min-width: 0;
}
.task-title {
  display: block;
  border: 0;
  padding: 0;
  min-height: 24px;
  background: transparent;
  color: var(--app-text);
  font-size: 14px;
  font-weight: 500;
  text-align: left;
  overflow-wrap: anywhere;
}
.completed .task-title {
  text-decoration: line-through;
  color: var(--app-muted);
}
.task-description {
  color: var(--app-muted);
  font-size: 12px;
  line-height: 1.6;
  margin: 5px 0;
  overflow-wrap: anywhere;
}
.task-description.folded {
  max-height: 4.8em;
  overflow: hidden;
}
.task-description-inner {
  line-height: 1.6;
}
.task-description-inner :deep(p) {
  margin: 0;
}
.task-description-inner :deep(ul),
.task-description-inner :deep(ol) {
  margin: 4px 0;
  padding-left: 20px;
}
.task-description-inner :deep(a) {
  color: var(--app-accent);
}
.task-fold {
  font-size: 11px;
  padding: 0;
  min-height: 28px;
}
.task-meta {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 7px 10px;
  margin-top: 6px;
  color: var(--app-muted);
  font-size: 11px;
}
.task-meta > span,
.task-meta > a {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  min-width: 0;
  overflow-wrap: anywhere;
}
.task-meta i {
  font-size: 12px;
  flex-shrink: 0;
}
.task-meta a {
  color: inherit;
  text-decoration: none;
}
.task-meta .overdue {
  color: var(--app-alert);
}
.area-chip {
  padding: 3px 7px;
  border-radius: 6px;
  background: color-mix(in srgb, var(--area-color) 12%, var(--app-card));
}
.area-chip > span {
  height: 7px;
  width: 7px;
  flex-shrink: 0;
  border-radius: 50%;
  background: var(--area-color);
}
.drag-handle {
  align-self: center;
  border: 0;
  padding: 10px;
  background: transparent;
  color: var(--app-muted);
  cursor: grab;
  touch-action: none;
}
.compact {
  padding: 9px 0;
}
.compact .task-title {
  font-size: 13px;
}
@media (max-width: 760px) {
  .task-title {
    min-height: 30px;
  }
  .task-check {
    min-width: 35px;
  }
  .drag-handle {
    padding: 12px 4px;
  }
  .task-fold {
    min-height: 36px;
  }
}
</style>
