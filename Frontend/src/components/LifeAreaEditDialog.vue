<script setup lang="ts">
import Dialog from 'primevue/dialog'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import ColorPicker from 'primevue/colorpicker'
import EmojiPicker from 'vue3-emoji-picker'
import 'vue3-emoji-picker/css'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { LifeAreaDTO } from '@/api/LifeAreasAPI'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { useApiError } from '@/composables/useApiError'

const DEFAULT_COLOR = '#3b82f6'

const props = defineProps<{
  area: LifeAreaDTO | null
}>()

const emit = defineEmits<{
  (e: 'close'): void
}>()

const { t } = useI18n()
const lifeAreasStore = useLifeAreasStore()
const apiError = useApiError()

const localName = ref(props.area?.name ?? '')
const localColor = ref(props.area?.color ?? DEFAULT_COLOR)
const localEmoji = ref(props.area?.emoji ?? null)

const isEdit = computed(() => !!props.area)
const canSave = computed(() => localName.value.trim().length > 0 && localColor.value.trim().length > 0)

const isSaveLoading = ref(false)
const isDeleteLoading = ref(false)
const errorText = ref('')

async function onSave() {
  if (!canSave.value) return
  errorText.value = ''
  isSaveLoading.value = true

  try {
    const color = localColor.value.trim()
    const hexColor = color.startsWith('#') ? color : '#' + color
    const emoji = localEmoji.value?.trim() || null
    if (isEdit.value) {
      await lifeAreasStore.updateLifeArea(props.area!.id, {
        name: localName.value.trim(),
        color: hexColor,
        emoji
      })
    } else {
      if (lifeAreasStore.isLimitReached) {
        errorText.value = t('lifeareas.editdialog.limitReached')
        return
      }

      await lifeAreasStore.createLifeArea({
        name: localName.value.trim(),
        color: hexColor,
        emoji
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
  if (!props.area) return
  errorText.value = ''
  isDeleteLoading.value = true
  try {
    await lifeAreasStore.deleteLifeArea(props.area.id)
    emit('close')
  } catch (e) {
    errorText.value = apiError.resolveMessage(e)
  } finally {
    isDeleteLoading.value = false
  }
}
</script>

<template>
  <Dialog
    modal
    :visible="true"
    :closable="false"
    :draggable="false"
    class="lifearea-edit-dialog"
    :pt="{
      root: { class: 'lifearea-edit-dialog-root' },
      header: { class: 'lifearea-edit-header' },
      content: { class: 'lifearea-edit-content' }
    }"
    @hide="emit('close')"
  >
    <template #header>
      <div class="lifearea-edit-header-left">
        <InputText
          id="lifearea-name"
          class="lifearea-edit-title"
          v-model="localName"
          :placeholder="t('lifeareas.editdialog.newLifeArea')"
          size="large"
        />
      </div>
      <div class="lifearea-edit-close-wrap">
        <Button
          icon="pi pi-times"
          severity="secondary"
          rounded
          variant="outlined"
          aria-label="Close"
          size="small"
          @click="emit('close')"
        />
      </div>
    </template>

    <div class="lifearea-edit-section">
      <label class="lifearea-edit-field-label" for="lifearea-color">{{ t('lifeareas.editdialog.color') }}</label>
      <div class="color-picker-wrap">
        <ColorPicker id="lifearea-color" v-model="localColor" format="hex" inline class="color-picker" />
      </div>
    </div>

    <div class="lifearea-edit-section">
      <label class="lifearea-edit-field-label">{{ t('lifeareas.editdialog.emoji') }}</label>
      <div class="emoji-picker-wrap">
        <EmojiPicker :native="true" @select="(e: { i: string }) => { localEmoji = e.i }" />
        <div v-if="localEmoji" class="lifearea-edit-emoji-preview">{{ localEmoji }}</div>
      </div>
    </div>

    <Message v-if="errorText.length" severity="error" :life="3000">
      {{ errorText }}
    </Message>

    <template #footer>
      <Button
        v-if="isEdit"
        :label="t('lifeareas.editdialog.delete')"
        severity="danger"
        :loading="isDeleteLoading"
        @click="onDelete"
      />
      <Button
        :label="isEdit ? t('lifeareas.editdialog.save') : t('lifeareas.editdialog.create')"
        :disabled="!canSave"
        :loading="isSaveLoading"
        @click="onSave"
      />
    </template>
  </Dialog>
</template>

<style scoped>
.lifearea-edit-dialog-root {
  width: 80%;
  max-width: 400px;
  border-radius: 16px;
  overflow: hidden;
}

:deep(.p-dialog-header.lifearea-edit-header) {
  display: flex;
  align-items: center;
  position: relative;
  padding: 1rem;
  padding-right: 3rem;
}

.lifearea-edit-header-left {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  min-width: 0;
  flex: 1;
}

.lifearea-edit-title {
  flex: 1;
  min-width: 0;
  border: none;
  box-shadow: none;
  background-color: transparent;
}

.lifearea-edit-close-wrap {
  position: absolute;
  top: 1rem;
  right: 1rem;
}

:deep(.p-dialog-content.lifearea-edit-content) {
  padding-top: 0.25rem;
}

.lifearea-edit-section {
  margin-top: 1rem;
}

.lifearea-edit-field-label {
  display: block;
  font-size: 0.875rem;
  color: var(--p-text-muted-color);
  margin-bottom: 0.5rem;
  text-align: center;
}

.color-picker-wrap {
  width: 100%;
  min-width: 0;
  border-radius: 12px;
  padding: 0.75rem;
  background: var(--p-content-background);
  border: 1px solid var(--p-content-border-color);
}

.color-picker {
  width: 100%;
  min-width: 0;
}

.emoji-picker-wrap {
  position: relative;
  border-radius: 12px;
  padding: 0.75rem;
  background: var(--p-content-background);
  border: 1px solid var(--p-content-border-color);
}

.emoji-picker-wrap :deep(.epr-main) {
  max-height: 220px;
}

.lifearea-edit-emoji-preview {
  margin-top: 0.5rem;
  font-size: 1.5rem;
  text-align: center;
}
</style>
