<script setup lang="ts">
import Drawer from 'primevue/drawer'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import DatePicker from 'primevue/datepicker'
import Message from 'primevue/message'
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { AddictionDTO } from '@/api/AddictionsAPI'
import { useAddictionsStore } from '@/stores/addictions'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { useGoalsStore } from '@/stores/goals'
import { useApiError } from '@/composables/useApiError'
import { toDateOnlyString } from '@/utils/dateOnly'

const ADDICTION_COLOR_OPTIONS = [
  '#ef4444', '#f97316', '#eab308', '#22c55e', '#3b82f6', '#a855f7', '#ec4899'
]

const props = defineProps<{
  addiction: AddictionDTO | null
}>()

const emit = defineEmits<{
  (e: 'close'): void
}>()

const { t } = useI18n()
const addictionsStore = useAddictionsStore()
const lifeAreasStore = useLifeAreasStore()
const goalsStore = useGoalsStore()
const apiError = useApiError()

const visible = ref(true)
watch(visible, (v) => {
  if (!v) emit('close')
})

const localTitle = ref(props.addiction?.title ?? '')
const localColor = ref(props.addiction?.color ?? ADDICTION_COLOR_OPTIONS[0] ?? '#ef4444')
const localGoalId = ref<string | null>(props.addiction?.goalId ?? null)
const localLifeAreaId = ref<string | null>(props.addiction?.lifeAreaId ?? null)
const localLastRelapseDate = ref<Date | null>(null)

const isEdit = computed(() => !!props.addiction)
const canSave = computed(() => localTitle.value.trim().length > 0)

const lifeAreaChipLabel = computed(() => {
  if (!localLifeAreaId.value) return t('lifeareas.field')
  const area = lifeAreasStore.lifeAreas.find(a => a.id === localLifeAreaId.value)
  return area?.name ?? t('lifeareas.field')
})

const hasLifeArea = computed(() => !!localLifeAreaId.value)

const goalChipLabel = computed(() => {
  if (!localGoalId.value) return t('goals.field')
  const goal = goalsStore.goalsSorted.find(g => g.id === localGoalId.value)
  return goal?.title ?? t('goals.field')
})

const hasGoal = computed(() => !!localGoalId.value)

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
    const request = {
      title: localTitle.value.trim(),
      color: localColor.value.trim(),
      goalId: localGoalId.value,
      lifeAreaId: localLifeAreaId.value,
      lastRelapseDate: localLastRelapseDate.value ? toDateOnlyString(localLastRelapseDate.value) : null
    }
    if (isEdit.value) {
      await addictionsStore.updateAddiction(props.addiction!.id, request)
    } else {
      await addictionsStore.createAddiction(request)
    }
    emit('close')
  } catch (e) {
    errorText.value = apiError.resolveMessage(e)
  } finally {
    isSaveLoading.value = false
  }
}

async function onDelete() {
  if (!props.addiction) return
  errorText.value = ''
  isDeleteLoading.value = true
  try {
    await addictionsStore.deleteAddiction(props.addiction.id)
    emit('close')
  } catch (e) {
    errorText.value = apiError.resolveMessage(e)
  } finally {
    isDeleteLoading.value = false
  }
}
</script>

<template>
  <Drawer v-model:visible="visible" position="bottom" class="addiction-drawer" style="height: auto; max-height: 85vh">
    <template #header>
      <div class="addiction-drawer-header" ref="titleWrap">
        <InputText v-model="localTitle" :placeholder="t('addictions.editdialog.newAddiction')"
          class="addiction-drawer-title-input" />
      </div>
    </template>

    <div class="addiction-drawer-section">
      <label class="addiction-drawer-label">{{ t('addictions.editdialog.color') }}</label>
      <div class="addiction-drawer-colors">
        <button v-for="color in ADDICTION_COLOR_OPTIONS" :key="color" type="button" class="addiction-color-chip"
          :class="{ selected: localColor === color }" :style="{ backgroundColor: color }"
          :aria-pressed="localColor === color" :aria-label="color" @click="localColor = color" />
      </div>
    </div>

    <div v-if="!isEdit" class="addiction-drawer-section">
      <label class="addiction-drawer-label">{{ t('addictions.editdialog.lastRelapseDate') }}</label>
      <DatePicker v-model="localLastRelapseDate" date-format="dd.mm.yy"
        :placeholder="t('addictions.editdialog.lastRelapseDatePlaceholder')" show-clear show-button-bar fluid />
    </div>

    <div class="addiction-drawer-chips">
      <Select v-model="localLifeAreaId" :options="lifeAreasStore.lifeAreas" option-label="name" option-value="id"
        show-clear :placeholder="t('lifeareas.field')" class="addiction-chip-select"
        :class="{ 'addiction-chip-select--active': hasLifeArea }">
        <template #value>
          <span class="addiction-chip-select-value">
            <i class="pi pi-objects-column" />
            {{ lifeAreaChipLabel }}
          </span>
        </template>
      </Select>

      <Select v-model="localGoalId" :options="goalsStore.goalsSorted" option-label="title" option-value="id"
        show-clear :placeholder="t('goals.field')" class="addiction-chip-select"
        :class="{ 'addiction-chip-select--active': hasGoal }">
        <template #value>
          <span class="addiction-chip-select-value">
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
      <div class="addiction-drawer-actions">
        <Button v-if="isEdit" icon="pi pi-trash" :label="t('addictions.editdialog.delete')" severity="danger"
          variant="text" size="small" :loading="isDeleteLoading" @click="onDelete" />
        <span v-else />
        <Button :label="isEdit ? t('addictions.editdialog.save') : t('addictions.editdialog.create')"
          :disabled="!canSave" :loading="isSaveLoading" icon="pi pi-check" @click="onSave" />
      </div>
    </template>
  </Drawer>
</template>

<style>
.p-drawer.addiction-drawer {
  border-radius: 1rem 1rem 0 0;
}

.addiction-drawer .p-drawer-header {
  position: relative;
  padding: 0.75rem 1.25rem;
  padding-top: 1.5rem;
}

.addiction-drawer .p-drawer-header::before {
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

.addiction-drawer-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex: 1;
  min-width: 0;
}

.p-inputtext.addiction-drawer-title-input {
  flex: 1;
  min-width: 0;
  border: none;
  box-shadow: none !important;
  background: transparent;
  font-size: 1.125rem;
  font-weight: 600;
  padding: 0.5rem 0;
}

.addiction-drawer .p-drawer-content {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding-bottom: 0.25rem;
}

.addiction-drawer-section {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.addiction-drawer-label {
  font-size: 0.8125rem;
  font-weight: 500;
  color: var(--p-text-muted-color);
  text-transform: uppercase;
  letter-spacing: 0.03em;
}

.addiction-drawer-colors {
  display: flex;
  justify-content: space-between;
}

.addiction-color-chip {
  width: 2rem;
  height: 2rem;
  border-radius: 50%;
  border: 2px solid transparent;
  padding: 0;
  cursor: pointer;
  transition: border-color 0.15s, transform 0.1s;
}

.addiction-color-chip:hover {
  transform: scale(1.1);
}

.addiction-color-chip.selected {
  border-color: var(--p-text-color);
  box-shadow: 0 0 0 1px var(--p-content-border-color);
}

.addiction-drawer-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  padding: 0.5rem 0;
  border-top: 1px solid var(--p-content-border-color);
}

.addiction-chip-select.p-select {
  border-radius: 999px;
  border-color: transparent;
  background: transparent;
  box-shadow: none;
  font-size: 0.8125rem;
  height: auto;
  padding-right: 0.5rem;
}

.addiction-chip-select.p-select .p-select-label {
  display: flex;
  align-items: center;
  padding: 0.25rem 0.5rem;
  font-size: 0.8125rem;
  color: var(--p-text-muted-color);
}

.addiction-chip-select.p-select .p-select-dropdown {
  display: none;
}

.addiction-chip-select--active.p-select {
  border-color: var(--p-primary-color);
}

.addiction-chip-select--active.p-select .p-select-label {
  color: var(--p-primary-color);
}

.addiction-chip-select-value {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  white-space: nowrap;
}

.addiction-chip-select-value i {
  font-size: 0.75rem;
}

.addiction-drawer .p-drawer-footer {
  border-top: 1px solid var(--p-content-border-color);
}

.addiction-drawer-actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
}
</style>
