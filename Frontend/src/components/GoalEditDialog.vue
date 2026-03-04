<script setup lang="ts">
import Drawer from 'primevue/drawer'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Textarea from 'primevue/textarea'
import Select from 'primevue/select'
import Popover from 'primevue/popover'
import DatePicker from 'primevue/datepicker'
import Message from 'primevue/message'
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { GoalDTO } from '@/api/GoalsAPI'
import { useGoalsStore } from '@/stores/goals'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { useApiError } from '@/composables/useApiError'

const props = defineProps<{
  goal: GoalDTO | null
}>()

const emit = defineEmits<{
  (e: 'close'): void
}>()

const { t, locale } = useI18n()
const goalsStore = useGoalsStore()
const lifeAreasStore = useLifeAreasStore()
const apiError = useApiError()

const visible = ref(true)
watch(visible, (v) => {
  if (!v) emit('close')
})

const localTitle = ref(props.goal?.title ?? '')
const localDescription = ref(props.goal?.description ?? '')
const localDueDate = ref<Date>(props.goal?.dueDate ? new Date(props.goal.dueDate) : new Date())
const localLifeAreaId = ref<string | null>(props.goal?.lifeAreaId ?? null)

const isEdit = computed(() => !!props.goal)
const canSave = computed(() => localTitle.value.trim().length > 0 && !!localDueDate.value)

const descriptionModel = computed({
  get() {
    return localDescription.value
  },
  set(value: string) {
    localDescription.value = value || ''
  }
})

const dueDateChipLabel = computed(() => {
  if (!localDueDate.value) return t('goals.dueDate')
  return localDueDate.value.toLocaleDateString(locale.value, { day: 'numeric', month: 'short', year: 'numeric' })
})

const lifeAreaChipLabel = computed(() => {
  if (!localLifeAreaId.value) return t('lifeareas.field')
  const area = lifeAreasStore.lifeAreas.find(a => a.id === localLifeAreaId.value)
  return area?.name ?? t('lifeareas.field')
})

const hasLifeArea = computed(() => !!localLifeAreaId.value)

const datePopover = ref<InstanceType<typeof Popover> | null>(null)

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
  errorText.value = ''
  isSaveLoading.value = true
  try {
    if (isEdit.value) {
      await goalsStore.updateGoal(props.goal!.id, {
        title: localTitle.value.trim(),
        description: localDescription.value.trim() || null,
        dueDate: localDueDate.value.toISOString(),
        lifeAreaId: localLifeAreaId.value
      })
    } else {
      await goalsStore.createGoal({
        title: localTitle.value.trim(),
        description: localDescription.value.trim() || null,
        dueDate: localDueDate.value.toISOString(),
        lifeAreaId: localLifeAreaId.value
      })
    }
    emit('close')
  } catch (e) {
    errorText.value = apiError.resolveMessage(e)
  } finally {
    isSaveLoading.value = false
  }
}

async function onDelete() {
  if (!props.goal) return
  errorText.value = ''
  isDeleteLoading.value = true
  try {
    await goalsStore.deleteGoal(props.goal.id)
    emit('close')
  } catch (e) {
    errorText.value = apiError.resolveMessage(e)
  } finally {
    isDeleteLoading.value = false
  }
}
</script>

<template>
  <Drawer v-model:visible="visible" position="bottom" class="goal-drawer" style="height: auto; max-height: 85vh">
    <template #header>
      <div class="goal-drawer-header" ref="titleWrap">
        <InputText v-model="localTitle" :placeholder="t('goals.newGoal')" class="goal-drawer-title-input" />
      </div>
    </template>

    <Textarea v-model="descriptionModel" :placeholder="t('tasks.description')" autoResize class="goal-drawer-textarea"
      :rows="3" fluid />

    <div class="goal-drawer-chips">
      <Button :label="dueDateChipLabel" icon="pi pi-calendar" size="small" severity="secondary" variant="outlined"
        class="goal-chip" @click="(e: Event) => datePopover?.toggle(e)" />
      <Popover ref="datePopover" append-to="body">
        <DatePicker v-model="localDueDate" inline />
      </Popover>

      <Select v-model="localLifeAreaId" :options="lifeAreasStore.lifeAreas" option-label="name" option-value="id"
        show-clear :placeholder="t('lifeareas.field')" class="goal-chip-select"
        :class="{ 'goal-chip-select--active': hasLifeArea }">
        <template #value>
          <span class="goal-chip-select-value">
            <i class="pi pi-objects-column" />
            {{ lifeAreaChipLabel }}
          </span>
        </template>
      </Select>
    </div>

    <Message v-if="errorText.length" severity="error" icon="pi pi-times-circle" :life="3000">
      {{ errorText }}
    </Message>

    <template #footer>
      <div class="goal-drawer-actions">
        <Button v-if="isEdit" icon="pi pi-trash" :label="t('goals.delete')" severity="danger" variant="text"
          size="small" :loading="isDeleteLoading" @click="onDelete" />
        <span v-else />
        <Button :label="isEdit ? t('goals.save') : t('goals.create')" :disabled="!canSave" :loading="isSaveLoading"
          icon="pi pi-check" @click="onSave" />
      </div>
    </template>
  </Drawer>
</template>

<style>
.p-drawer.goal-drawer {
  border-radius: 1rem 1rem 0 0;
}

.goal-drawer .p-drawer-header {
  position: relative;
  padding: 0.75rem 1.25rem;
  padding-top: 1.5rem;
}

.goal-drawer .p-drawer-header::before {
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

.goal-drawer-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex: 1;
  min-width: 0;
}

.p-inputtext.goal-drawer-title-input {
  flex: 1;
  min-width: 0;
  border: none;
  box-shadow: none !important;
  background: transparent;
  font-size: 1.125rem;
  font-weight: 600;
  padding: 0.5rem 0;
}

.goal-drawer .p-drawer-content {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding-bottom: 0.25rem;
}

.goal-drawer-textarea.p-textarea {
  border: none;
  box-shadow: none !important;
  background: transparent;
  font-size: 1rem;
  line-height: 1.6;
  padding: 0.5rem 0;
}

.goal-drawer-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  padding: 0.5rem 0;
  border-top: 1px solid var(--p-content-border-color);
}

.goal-chip {
  border-radius: 999px !important;
  font-size: 0.8125rem !important;
}

.goal-chip-select.p-select {
  border-radius: 999px;
  border-color: transparent;
  background: transparent;
  box-shadow: none;
  font-size: 0.8125rem;
  height: auto;
  padding-right: 0.5rem;
}

.goal-chip-select.p-select .p-select-label {
  display: flex;
  align-items: center;
  padding: 0.25rem 0.5rem;
  font-size: 0.8125rem;
  color: var(--p-text-muted-color);
}

.goal-chip-select.p-select .p-select-dropdown {
  display: none;
}

.goal-chip-select--active.p-select {
  border-color: var(--p-primary-color);
}

.goal-chip-select--active.p-select .p-select-label {
  color: var(--p-primary-color);
}

.goal-chip-select-value {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  white-space: nowrap;
}

.goal-chip-select-value i {
  font-size: 0.75rem;
}

.goal-drawer .p-drawer-footer {
  border-top: 1px solid var(--p-content-border-color);
}

.goal-drawer-actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
}
</style>
