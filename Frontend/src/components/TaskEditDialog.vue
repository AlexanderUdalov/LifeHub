<script setup lang="ts">
import Drawer from 'primevue/drawer'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Textarea from 'primevue/textarea'
import Checkbox from 'primevue/checkbox'
import Message from 'primevue/message'
import Popover from 'primevue/popover'
import Select from 'primevue/select'
import { computed, onMounted, ref, watch } from 'vue'
import { type CreateTaskRequest, type TaskDTO, type UpdateTaskRequest } from '@/api/TasksAPI'
import { useI18n } from 'vue-i18n'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { useGoalsStore } from '@/stores/goals'
import DateAndRecurrencePicker from '@/components/DateAndRecurrencePicker.vue'
import { useRecurrenceFormatter } from '@/composables/useRecurrenceFormatter'
import { useApiError } from '@/composables/useApiError'
import { useTasksStore } from '@/stores/tasks'
import { startOfDay, toUtcDateOnlyIso } from '@/utils/dateOnly'

const { t, locale } = useI18n()

const props = defineProps<{
  task: TaskDTO | null
}>()

const emit = defineEmits<{
  (e: 'close'): void
}>()

const apiError = useApiError()
const tasksStore = useTasksStore()
const lifeAreasStore = useLifeAreasStore()
const goalsStore = useGoalsStore()

const visible = ref(true)
watch(visible, (v) => {
  if (!v) emit('close')
})

const localTask = ref<TaskDTO>({
  id: props.task?.id ?? '',
  title: props.task?.title ?? '',
  description: props.task?.description ?? null,
  dueDate: props.task?.dueDate ? props.task.dueDate : null,
  completionDate: props.task?.completionDate ? props.task.completionDate : null,
  recurrenceRule: props.task?.recurrenceRule ?? null,
  goalId: props.task?.goalId ? props.task.goalId : null,
  lifeAreaId: props.task?.lifeAreaId ? props.task.lifeAreaId : null,
  sortOrder: props.task?.sortOrder ?? null
})

const dueDateAsDate = computed<Date | null>({
  get() {
    return localTask.value.dueDate ? startOfDay(new Date(localTask.value.dueDate)) : null
  },
  set(value) {
    localTask.value.dueDate = value ? toUtcDateOnlyIso(value) : null
  }
})

const isEdit = computed(() => !!props.task)
const canSave = computed(() => localTask.value.title.trim().length > 0)

const completedModel = computed({
  get() {
    return localTask.value.completionDate != null
  },
  set(value) {
    localTask.value.completionDate = value ? new Date().toISOString() : null
  }
})

const descriptionModel = computed({
  get() {
    return localTask.value.description ?? ''
  },
  set(value) {
    localTask.value.description = value || null
  }
})

const { formatRecurrence } = useRecurrenceFormatter()

const dateChipLabel = computed(() => {
  const d = dueDateAsDate.value
  if (!d && !localTask.value.recurrenceRule) return t('tasks.recurrence.dateAndRepeat')
  const dateStr = d
    ? d.toLocaleDateString(locale.value, { day: 'numeric', month: 'short' })
    : ''
  const recurStr = formatRecurrence(localTask.value.recurrenceRule, localTask.value.dueDate)
  if (dateStr && recurStr) return `${dateStr} · ${recurStr}`
  return dateStr || recurStr || t('tasks.recurrence.dateAndRepeat')
})

const hasDate = computed(() => !!localTask.value.dueDate || !!localTask.value.recurrenceRule)

const lifeAreaChipLabel = computed(() => {
  if (!localTask.value.lifeAreaId) return t('lifeareas.field')
  const area = lifeAreasStore.lifeAreas.find(a => a.id === localTask.value.lifeAreaId)
  return area?.name ?? t('lifeareas.field')
})

const hasLifeArea = computed(() => !!localTask.value.lifeAreaId)

const goalChipLabel = computed(() => {
  if (!localTask.value.goalId) return t('goals.field')
  const goal = goalsStore.goalsSorted.find(g => g.id === localTask.value.goalId)
  return goal?.title ?? t('goals.field')
})

const hasGoal = computed(() => !!localTask.value.goalId)

watch(
  () => localTask.value.goalId,
  (goalId) => {
    if (!goalId) return
    const goal = goalsStore.goalsSorted.find(g => g.id === goalId)
    if (goal && localTask.value.lifeAreaId !== goal.lifeAreaId) {
      localTask.value.lifeAreaId = goal.lifeAreaId
    }
  },
  { immediate: true }
)

const dateRecurrencePopover = ref<InstanceType<typeof Popover> | null>(null)

const titleWrap = ref<HTMLElement | null>(null)

onMounted(() => {
  setTimeout(() => {
    titleWrap.value?.querySelector('input')?.focus()
  }, 300)
})

const isSaveLoading = ref(false)
const isDeleteLoading = ref(false)
const errorText = ref('')

async function onSave() {
  if (!canSave.value) return
  try {
    errorText.value = ''
    isSaveLoading.value = true
    if (isEdit.value) {
      const request: UpdateTaskRequest = {
        title: localTask.value.title,
        description: localTask.value.description,
        dueDate: localTask.value.dueDate,
        completionDate: localTask.value.completionDate,
        recurrenceRule: localTask.value.recurrenceRule,
        goalId: localTask.value.goalId,
        lifeAreaId: localTask.value.lifeAreaId,
        sortOrder: localTask.value.dueDate ? null : (localTask.value.sortOrder ?? null)
      }
      await tasksStore.editTask(localTask.value.id, request)
    } else {
      const request: CreateTaskRequest = {
        title: localTask.value.title,
        description: localTask.value.description,
        dueDate: localTask.value.dueDate,
        recurrenceRule: localTask.value.recurrenceRule,
        goalId: localTask.value.goalId,
        lifeAreaId: localTask.value.lifeAreaId
      }
      await tasksStore.createNewTask(request)
    }
    emit('close')
  } catch (e) {
    errorText.value = apiError.resolveMessage(e)
  } finally {
    isSaveLoading.value = false
  }
}

async function onDelete() {
  try {
    errorText.value = ''
    isDeleteLoading.value = true
    await tasksStore.removeTask(localTask.value.id)
    emit('close')
  } catch (e) {
    errorText.value = apiError.resolveMessage(e)
  } finally {
    isDeleteLoading.value = false
  }
}
</script>

<template>
  <Drawer v-model:visible="visible" position="bottom" class="task-drawer" style="height: auto; max-height: 85vh">
    <template #header>
      <div class="task-drawer-header" ref="titleWrap">
        <Checkbox v-if="isEdit" binary v-model="completedModel" />
        <InputText v-model="localTask.title" :placeholder="t('tasks.editdialog.newTask')"
          class="task-drawer-title-input" :class="{ completed: completedModel }" />
      </div>
    </template>

    <Textarea v-model="descriptionModel" :placeholder="t('tasks.description')" autoResize class="task-drawer-textarea"
      :rows="3" fluid />

    <div class="task-drawer-chips">
      <Button :label="dateChipLabel" icon="pi pi-calendar" size="small" :severity="hasDate ? undefined : 'secondary'"
        :variant="hasDate ? 'outlined' : 'text'" class="task-chip"
        @click="(e: Event) => dateRecurrencePopover?.toggle(e)" />
      <Popover ref="dateRecurrencePopover" append-to="body">
        <DateAndRecurrencePicker :date="dueDateAsDate" :recurrence-rule="localTask.recurrenceRule"
          @update:date="(v) => { localTask.dueDate = v ? toUtcDateOnlyIso(v) : null }"
          @update:recurrence-rule="(v) => { localTask.recurrenceRule = v }" @close="dateRecurrencePopover?.hide()" />
      </Popover>

      <Select v-if="!hasGoal" v-model="localTask.lifeAreaId" :options="lifeAreasStore.lifeAreas" option-label="name" option-value="id"
        show-clear :placeholder="t('lifeareas.field')" class="task-chip-select"
        :class="{ 'task-chip-select--active': hasLifeArea }">
        <template #value>
          <span class="task-chip-select-value">
            <i class="pi pi-chart-pie" />
            {{ lifeAreaChipLabel }}
          </span>
        </template>
      </Select>

      <Select v-model="localTask.goalId" :options="goalsStore.goalsSorted" option-label="title" option-value="id"
        show-clear :placeholder="t('goals.field')" class="task-chip-select"
        :class="{ 'task-chip-select--active': hasGoal }">
        <template #value>
          <span class="task-chip-select-value">
            <i class="pi pi-bullseye" />
            {{ goalChipLabel }}
          </span>
        </template>
      </Select>
    </div>

    <Message v-if="errorText.length" severity="error" icon="pi pi-times-circle" :life="3000">
      {{ errorText }}
    </Message>

    <template #footer>
      <div class="task-drawer-actions">
        <Button v-if="isEdit" icon="pi pi-trash" :label="t('tasks.editdialog.delete')" severity="danger" variant="text"
          size="small" :loading="isDeleteLoading" @click="onDelete" />
        <span v-else />
        <Button :label="isEdit ? t('tasks.editdialog.save') : t('tasks.editdialog.create')" :disabled="!canSave"
          :loading="isSaveLoading" icon="pi pi-check" @click="onSave" />
      </div>
    </template>
  </Drawer>
</template>

<style>
.p-drawer.task-drawer {
  border-radius: 1rem 1rem 0 0;
}

.task-drawer .p-drawer-header {
  position: relative;
  padding: 0.75rem 1.25rem;
  padding-top: 1.5rem;
}

.task-drawer .p-drawer-header::before {
  content: '';
  position: absolute;
  top: 0.5rem;
  left: 50%;
  transform: translateX(-50%);
  width: 2.5rem;
  height: 0.25rem;
  background: var(--p-content-border-color);
  border-radius: 999px;
}

.task-drawer-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex: 1;
  min-width: 0;
}

.p-inputtext.task-drawer-title-input {
  flex: 1;
  min-width: 0;
  border: none;
  box-shadow: none !important;
  background: transparent;
  font-size: 1.125rem;
  font-weight: 600;
  padding: 0.5rem 0;
}

.p-inputtext.task-drawer-title-input.completed {
  text-decoration: line-through;
  color: var(--p-text-muted-color);
}

.task-drawer .p-drawer-content {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding-bottom: 0.25rem;
}

.task-drawer-textarea.p-textarea {
  border: none;
  box-shadow: none !important;
  background: transparent;
  font-size: 1rem;
  line-height: 1.6;
  padding: 0.5rem 0;
}

.task-drawer-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  padding: 0.5rem 0;
  border-top: 1px solid var(--p-content-border-color);
}

.task-chip {
  border-radius: 999px !important;
  font-size: 0.8125rem !important;
}

.task-chip-select.p-select {
  border-radius: 999px;
  border-color: transparent;
  background: transparent;
  box-shadow: none;
  font-size: 0.8125rem;
  height: auto;
  padding-right: 0.5rem;
}

.task-chip-select.p-select .p-select-label {
  display: flex;
  align-items: center;
  padding: 0.25rem 0.5rem;
  font-size: 0.8125rem;
  color: var(--p-text-muted-color);
}

.task-chip-select.p-select .p-select-dropdown {
  display: none;
}

.task-chip-select--active.p-select {
  border-color: var(--p-primary-color);
  --p-select-clear-icon-color: var(--p-primary-color);
}

.task-chip-select--active.p-select .p-select-label {
  color: var(--p-primary-color);
}

.task-chip-select-value {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  white-space: nowrap;
}

.task-chip-select-value i {
  font-size: 0.75rem;
}

.task-drawer .p-drawer-footer {
  border-top: 1px solid var(--p-content-border-color);
}

.task-drawer-actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
}
</style>
