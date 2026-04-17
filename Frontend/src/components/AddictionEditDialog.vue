<script setup lang="ts">
import Button from 'primevue/button'
import ToggleSwitch from 'primevue/toggleswitch'
import InputText from 'primevue/inputtext'
import Textarea from 'primevue/textarea'
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
import BaseDrawer from '@/components/base/BaseDrawer.vue'

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
const localDescription = ref(props.addiction?.description ?? '')
const localColor = ref(props.addiction?.color ?? ADDICTION_COLOR_OPTIONS[0] ?? '#ef4444')
const localGoalId = ref<string | null>(props.addiction?.goalId ?? null)
const localLifeAreaId = ref<string | null>(props.addiction?.lifeAreaId ?? null)
const localLastRelapseAt = ref<Date | null>(null)
const localIsNsfw = ref(!!props.addiction?.isNsfw)

watch(
  () => props.addiction?.id,
  () => {
    localIsNsfw.value = !!props.addiction?.isNsfw
  }
)

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

const effectiveColor = computed(() => {
  if (localLifeAreaId.value) {
    const area = lifeAreasStore.lifeAreas.find(a => a.id === localLifeAreaId.value)
    if (area?.color?.trim()) return area.color.trim()
  }
  return localColor.value.trim()
})

watch(localLifeAreaId, (id) => {
  if (!id) return
  const area = lifeAreasStore.lifeAreas.find(a => a.id === id)
  if (area?.color?.trim()) localColor.value = area.color.trim()
})

const titleWrap = ref<HTMLElement | null>(null)

onMounted(() => {
  if (props.addiction?.lifeAreaId) {
    const area = lifeAreasStore.lifeAreas.find(a => a.id === props.addiction!.lifeAreaId)
    if (area?.color?.trim()) localColor.value = area.color.trim()
  }
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
      description: localDescription.value.trim() || null,
      color: effectiveColor.value,
      goalId: localGoalId.value,
      lifeAreaId: localLifeAreaId.value,
      isNsfw: localIsNsfw.value,
      lastRelapseAt:
        !isEdit.value && localLastRelapseAt.value ? localLastRelapseAt.value.toISOString() : null
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
  <BaseDrawer v-model:visible="visible" class="addiction-drawer">
    <template #header>
      <div class="addiction-drawer-header" ref="titleWrap">
        <InputText v-model="localTitle" :placeholder="t('addictions.editdialog.newAddiction')"
          class="addiction-drawer-title-input" />
      </div>
    </template>

    <div class="ds-chip-row">
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

      <Select v-model="localGoalId" :options="goalsStore.goalsSorted" option-label="title" option-value="id"
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

    <div v-if="!localLifeAreaId" class="addiction-drawer-section">
      <label class="addiction-drawer-label">{{ t('addictions.editdialog.color') }}</label>
      <div class="addiction-drawer-colors">
        <button v-for="color in ADDICTION_COLOR_OPTIONS" :key="color" type="button" class="addiction-color-chip"
          :class="{ selected: localColor === color }" :style="{ backgroundColor: color }"
          :aria-pressed="localColor === color" :aria-label="color" @click="localColor = color" />
      </div>
    </div>

    <div class="addiction-drawer-section addiction-drawer-section--row">
      <div class="addiction-nsfw-label">
        <label class="addiction-drawer-label" for="addiction-nsfw">{{ t('addictions.editdialog.nsfw') }}</label>
        <span class="addiction-nsfw-hint">{{ t('addictions.editdialog.nsfwHint') }}</span>
      </div>
      <ToggleSwitch id="addiction-nsfw" v-model="localIsNsfw" />
    </div>

    <div class="addiction-drawer-section">
      <label class="addiction-drawer-label">{{ t('addictions.editdialog.description') }}</label>
      <Textarea
        v-model="localDescription"
        :placeholder="t('addictions.editdialog.descriptionPlaceholder')"
        autoResize
        rows="4"
        class="addiction-description-textarea w-full"
      />
    </div>

    <div v-if="!isEdit" class="addiction-drawer-section">
      <label class="addiction-drawer-label">{{ t('addictions.editdialog.lastRelapseDateTime') }}</label>
      <DatePicker v-model="localLastRelapseAt" showTime hourFormat="24" date-format="dd.mm.yy"
        :placeholder="t('addictions.editdialog.lastRelapseDateTimePlaceholder')" show-clear show-button-bar fluid />
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
  </BaseDrawer>
</template>

<style scoped>
.addiction-description-textarea {
  width: 100%;
  min-height: 6rem;
}

.addiction-drawer-section--row {
  display: flex;
  flex-direction: row;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
}

.addiction-nsfw-label {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  min-width: 0;
}

.addiction-nsfw-hint {
  font-size: 0.8rem;
  color: var(--p-text-muted-color);
  line-height: 1.3;
}
</style>
