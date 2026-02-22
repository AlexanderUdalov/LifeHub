<script setup lang="ts">
import Dialog from 'primevue/dialog'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { AddictionDTO } from '@/api/AddictionsAPI'
import { useAddictionsStore } from '@/stores/addictions'
import { useApiError } from '@/composables/useApiError'

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
const apiError = useApiError()

const localTitle = ref(props.addiction?.title ?? '')
const localColor = ref(props.addiction?.color ?? ADDICTION_COLOR_OPTIONS[0] ?? '#ef4444')

const isEdit = computed(() => !!props.addiction)
const canSave = computed(() => localTitle.value.trim().length > 0)

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
      goalId: null as string | null
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
  <Dialog modal :visible="true" :closable="false" :draggable="false" style="width: 90vw; max-width: 400px"
    :pt="{ header: { class: 'addiction-edit-header-wrap' }, content: { class: 'addiction-edit-content' } }"
    @hide="emit('close')">
    <template #header>
      <div class="addiction-edit-header">
        <InputText id="addiction-title" v-model="localTitle" class="addiction-edit-title"
          :placeholder="t('addictions.editdialog.newAddiction')" size="large" />
        <Button icon="pi pi-times" severity="secondary" rounded variant="outlined" aria-label="Cancel" size="small"
          @click="emit('close')" />
      </div>
    </template>

    <div class="form-field">
      <label class="field-label">{{ t('addictions.editdialog.color') }}</label>
      <div class="color-chips">
        <button v-for="color in ADDICTION_COLOR_OPTIONS" :key="color" type="button" class="color-chip"
          :class="{ selected: localColor === color }" :style="{ backgroundColor: color }"
          :aria-pressed="localColor === color" :aria-label="color" @click="localColor = color" />
      </div>
    </div>

    <Message v-if="errorText.length" severity="error" icon="pi pi-times-circle" :life="3000">
      {{ errorText }}
    </Message>

    <template #footer>
      <Button v-if="isEdit" :label="t('addictions.editdialog.delete')" severity="danger" :loading="isDeleteLoading"
        @click="onDelete" />
      <Button :label="isEdit ? t('addictions.editdialog.save') : t('addictions.editdialog.create')" :disabled="!canSave"
        :loading="isSaveLoading" @click="onSave" />
    </template>
  </Dialog>
</template>

<style scoped>
.addiction-edit-header-wrap {
  width: 100%;
}

.addiction-edit-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  width: 100%;
}

.addiction-edit-title {
  flex: 1;
  min-width: 0;
  border: none;
  box-shadow: none;
  background-color: transparent;
}

.form-field {
  margin-top: 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  width: 100%;
}

.field-label {
  font-size: 0.875rem;
  color: var(--p-text-muted-color);
}

.color-chips {
  display: flex;
  flex-wrap: wrap;
  justify-content: space-between;
  gap: 0.5rem;
  width: 100%;
}

.color-chip {
  width: 2rem;
  height: 2rem;
  border-radius: 50%;
  border: 2px solid transparent;
  padding: 0;
  cursor: pointer;
  transition: border-color 0.15s, transform 0.1s;
}

.color-chip:hover {
  transform: scale(1.1);
}

.color-chip.selected {
  border-color: var(--p-text-color);
  box-shadow: 0 0 0 1px var(--p-content-border-color);
}
</style>
