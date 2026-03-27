<script setup lang="ts">
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
import BaseDrawer from '@/components/base/BaseDrawer.vue'

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
  <BaseDrawer v-model:visible="visible" class="goal-drawer">
    <template #header>
      <div class="ds-drawer-title-row" ref="titleWrap">
        <InputText v-model="localTitle" :placeholder="t('goals.newGoal')" class="ds-drawer-title-input" />
      </div>
    </template>

    <Textarea v-model="descriptionModel" :placeholder="t('tasks.description')" autoResize class="ds-drawer-textarea"
      :rows="3" fluid />

    <div class="ds-chip-row">
      <Button :label="dueDateChipLabel" icon="pi pi-calendar" size="small" severity="secondary" variant="outlined"
        class="ds-chip-button" @click="(e: Event) => datePopover?.toggle(e)" />
      <Popover ref="datePopover" append-to="body">
        <DatePicker v-model="localDueDate" inline />
      </Popover>

      <Select v-model="localLifeAreaId" :options="lifeAreasStore.lifeAreas" option-label="name" option-value="id"
        show-clear :placeholder="t('lifeareas.field')" class="ds-chip-select"
        :class="{ 'ds-chip-select--active': hasLifeArea }">
        <template #value>
          <span class="ds-chip-select-value">
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
      <div class="ds-drawer-actions">
        <Button v-if="isEdit" icon="pi pi-trash" :label="t('goals.delete')" severity="danger" variant="text"
          size="small" :loading="isDeleteLoading" @click="onDelete" />
        <span v-else />
        <Button :label="isEdit ? t('goals.save') : t('goals.create')" :disabled="!canSave" :loading="isSaveLoading"
          icon="pi pi-check" @click="onSave" />
      </div>
    </template>
  </BaseDrawer>
</template>

<style>
</style>
