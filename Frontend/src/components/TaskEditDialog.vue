<script setup lang="ts">
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Textarea from 'primevue/textarea'
import Checkbox from 'primevue/checkbox'
import Message from 'primevue/message'
import Select from 'primevue/select'
import { computed, onMounted, ref, watch } from 'vue'
import { type CreateTaskRequest, type TaskDTO, type UpdateTaskRequest } from '@/api/TasksAPI'
import { useI18n } from 'vue-i18n'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { useGoalsStore } from '@/stores/goals'
import TaskDateRecurrenceDrawer from '@/components/TaskDateRecurrenceDrawer.vue'
import { useRecurrenceFormatter } from '@/composables/useRecurrenceFormatter'
import { useApiError } from '@/composables/useApiError'
import { useTasksStore } from '@/stores/tasks'
import { startOfDay, toUtcDateOnlyIso } from '@/utils/dateOnly'
import BaseDrawer from '@/components/base/BaseDrawer.vue'

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

const dateRecurrenceDrawerOpen = ref(false)

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
  <BaseDrawer v-model:visible="visible" class="task-drawer">
    <template #header>
      <div class="ds-drawer-title-row" ref="titleWrap">
        <Checkbox v-if="isEdit" binary v-model="completedModel" />
        <InputText v-model="localTask.title" :placeholder="t('tasks.editdialog.newTask')"
          class="ds-drawer-title-input task-drawer-title-input" :class="{ completed: completedModel }" />
      </div>
    </template>

    <Textarea v-model="descriptionModel" :placeholder="t('tasks.description')" autoResize class="ds-drawer-textarea"
      :rows="3" fluid />

    <div class="ds-chip-row">
      <Button :label="dateChipLabel" icon="pi pi-calendar" size="small" :severity="hasDate ? undefined : 'secondary'"
        :variant="hasDate ? 'outlined' : 'text'" class="ds-chip-button"
        @click="dateRecurrenceDrawerOpen = true" />
      <TaskDateRecurrenceDrawer v-model:visible="dateRecurrenceDrawerOpen" :date="dueDateAsDate"
        :recurrence-rule="localTask.recurrenceRule"
        @update:date="(v) => { localTask.dueDate = v ? toUtcDateOnlyIso(v) : null }"
        @update:recurrence-rule="(v) => { localTask.recurrenceRule = v }" />

      <Select v-if="!hasGoal" v-model="localTask.lifeAreaId" :options="lifeAreasStore.lifeAreas" option-label="name" option-value="id"
        show-clear :placeholder="t('lifeareas.field')" class="ds-chip-select"
        :class="{ 'ds-chip-select--active': hasLifeArea }">
        <template #value>
          <span class="ds-chip-select-value">
            <i class="pi pi-chart-pie" />
            {{ lifeAreaChipLabel }}
          </span>
        </template>
      </Select>

      <Select v-model="localTask.goalId" :options="goalsStore.goalsSorted" option-label="title" option-value="id"
        show-clear :placeholder="t('goals.field')" class="ds-chip-select"
        :class="{ 'ds-chip-select--active': hasGoal }">
        <template #value>
          <span class="ds-chip-select-value">
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
      <div class="ds-drawer-actions">
        <Button v-if="isEdit" icon="pi pi-trash" :label="t('tasks.editdialog.delete')" severity="danger" variant="text"
          size="small" :loading="isDeleteLoading" @click="onDelete" />
        <span v-else />
        <Button :label="isEdit ? t('tasks.editdialog.save') : t('tasks.editdialog.create')" :disabled="!canSave"
          :loading="isSaveLoading" icon="pi pi-check" @click="onSave" />
      </div>
    </template>
  </BaseDrawer>
</template>

<style>
.p-inputtext.task-drawer-title-input.completed {
  text-decoration: line-through;
  color: var(--p-text-muted-color);
}
</style>
