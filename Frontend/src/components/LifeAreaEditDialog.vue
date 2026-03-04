<script setup lang="ts">
import Drawer from 'primevue/drawer'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import ColorPicker from 'primevue/colorpicker'
import EmojiPicker from 'vue3-emoji-picker'
import 'vue3-emoji-picker/css'
import { computed, onMounted, ref, watch } from 'vue'
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

const visible = ref(true)
watch(visible, (v) => {
  if (!v) emit('close')
})

const localName = ref(props.area?.name ?? '')
const localColor = ref(props.area?.color ?? DEFAULT_COLOR)
const localEmoji = ref(props.area?.emoji ?? null)

const emojiTheme = ref<'light' | 'dark' | 'auto'>('auto')

onMounted(() => {
  const isDark = document.documentElement.classList.contains('p-dark')
  emojiTheme.value = isDark ? 'dark' : 'light'
})

const isEdit = computed(() => !!props.area)
const canSave = computed(() => localName.value.trim().length > 0 && localColor.value.trim().length > 0)

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
  <Drawer v-model:visible="visible" position="bottom" class="lifearea-drawer" style="height: auto; max-height: 85vh">
    <template #header>
      <div class="lifearea-drawer-header" ref="titleWrap">
        <span v-if="localEmoji" class="lifearea-drawer-emoji">{{ localEmoji }}</span>
        <InputText v-model="localName" :placeholder="t('lifeareas.editdialog.newLifeArea')"
          class="lifearea-drawer-title-input" />
      </div>
    </template>

    <div class="lifearea-drawer-section">
      <label class="lifearea-drawer-label">{{ t('lifeareas.editdialog.color') }}</label>
      <ColorPicker v-model="localColor" format="hex" inline class="lifearea-drawer-color-picker" />
    </div>

    <div class="lifearea-drawer-section">
      <div class="lifearea-drawer-emoji-header">
        <span class="lifearea-drawer-label">{{ t('lifeareas.editdialog.emoji') }}</span>
        <span v-if="localEmoji" class="lifearea-drawer-emoji-preview">{{ localEmoji }}</span>
      </div>
      <EmojiPicker :native="false" :theme="emojiTheme" :disable-skin-tones="true"
        @select="(e: { i: string }) => { localEmoji = e.i }" />
    </div>

    <Message v-if="errorText.length" severity="error" icon="pi pi-times-circle" :life="3000">
      {{ errorText }}
    </Message>

    <template #footer>
      <div class="lifearea-drawer-actions">
        <Button v-if="isEdit" icon="pi pi-trash" :label="t('lifeareas.editdialog.delete')" severity="danger"
          variant="text" size="small" :loading="isDeleteLoading" @click="onDelete" />
        <span v-else />
        <Button :label="isEdit ? t('lifeareas.editdialog.save') : t('lifeareas.editdialog.create')"
          :disabled="!canSave" :loading="isSaveLoading" icon="pi pi-check" @click="onSave" />
      </div>
    </template>
  </Drawer>
</template>

<style>
.p-drawer.lifearea-drawer {
  border-radius: 1rem 1rem 0 0;
}

.lifearea-drawer .p-drawer-header {
  position: relative;
  padding: 0.75rem 1.25rem;
  padding-top: 1.5rem;
}

.lifearea-drawer .p-drawer-header::before {
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

.lifearea-drawer-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex: 1;
  min-width: 0;
}

.lifearea-drawer-emoji {
  font-size: 1.5rem;
  line-height: 1;
}

.p-inputtext.lifearea-drawer-title-input {
  flex: 1;
  min-width: 0;
  border: none;
  box-shadow: none !important;
  background: transparent;
  font-size: 1.125rem;
  font-weight: 600;
  padding: 0.5rem 0;
}

.lifearea-drawer .p-drawer-content {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding-bottom: 0.25rem;
}

.lifearea-drawer-section {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.lifearea-drawer-label {
  font-size: 0.8125rem;
  font-weight: 500;
  color: var(--p-text-muted-color);
  text-transform: uppercase;
  letter-spacing: 0.03em;
}

.lifearea-drawer-color-picker {
  width: 100%;
  min-width: 0;
}

.lifearea-drawer-emoji-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.lifearea-drawer-emoji-preview {
  font-size: 1.5rem;
}

.lifearea-drawer .p-drawer-footer {
  border-top: 1px solid var(--p-content-border-color);
}

.lifearea-drawer-actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
}
</style>
