<script setup lang="ts">
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { LifeAreaDTO } from '@/api/LifeAreasAPI'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { useApiError } from '@/composables/useApiError'
import { LOGO_COLORS, isLogoColor, normalizeHex } from '@/constants/logoColors'
import BaseDrawer from '@/components/base/BaseDrawer.vue'

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

function getInitialColor(): string {
  const areaColor = props.area?.color
  if (areaColor && isLogoColor(areaColor)) return normalizeHex(areaColor)
  const used = new Set(
    lifeAreasStore.lifeAreas.map((a) => normalizeHex(a.color))
  )
  const first = LOGO_COLORS.find((c) => !used.has(c))
  return first ?? LOGO_COLORS[0]
}

const localName = ref(props.area?.name ?? '')
const localColor = ref(getInitialColor())
const localEmoji = ref(props.area?.emoji ?? '')

const isEdit = computed(() => !!props.area)
const canSave = computed(() => localName.value.trim().length > 0 && localColor.value.trim().length > 0)

/** Logo colors not used by other life areas (current area keeps its color). */
const availableColors = computed(() => {
  const usedByOthers = new Set(
    lifeAreasStore.lifeAreas
      .filter((a) => !props.area || a.id !== props.area.id)
      .map((a) => normalizeHex(a.color))
  )
  return LOGO_COLORS.filter((c) => !usedByOthers.has(c))
})

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
  <BaseDrawer v-model:visible="visible" class="lifearea-drawer">
    <template #header>
      <div class="ds-drawer-title-row" ref="titleWrap">
        <span v-if="localEmoji" class="lifearea-drawer-emoji">{{ localEmoji }}</span>
        <InputText v-model="localName" :placeholder="t('lifeareas.editdialog.newLifeArea')"
          class="ds-drawer-title-input" />
      </div>
    </template>

    <div class="ds-drawer-section">
      <label class="ds-drawer-label">{{ t('lifeareas.editdialog.color') }}</label>
      <div class="lifearea-drawer-colors" role="listbox" :aria-label="t('lifeareas.editdialog.color')">
        <button v-for="color in availableColors" :key="color" type="button" class="ds-color-swatch lifearea-color-chip"
          :class="{ 'is-selected': normalizeHex(localColor) === color }" :style="{ backgroundColor: color }"
          :aria-pressed="normalizeHex(localColor) === color" :aria-label="color" @click="localColor = color" />
      </div>
    </div>

    <div class="ds-drawer-section">
      <label class="ds-drawer-label">{{ t('lifeareas.editdialog.emoji') }}</label>
      <InputText v-model="localEmoji" :placeholder="t('lifeareas.editdialog.emojiPlaceholder')"
        class="lifearea-drawer-emoji-input" />
    </div>

    <Message v-if="errorText.length" severity="error" icon="pi pi-times-circle" :life="3000">
      {{ errorText }}
    </Message>

    <template #footer>
      <div class="ds-drawer-actions">
        <Button v-if="isEdit" icon="pi pi-trash" :label="t('lifeareas.editdialog.delete')" severity="danger"
          variant="text" size="small" :loading="isDeleteLoading" @click="onDelete" />
        <span v-else />
        <Button :label="isEdit ? t('lifeareas.editdialog.save') : t('lifeareas.editdialog.create')" :disabled="!canSave"
          :loading="isSaveLoading" icon="pi pi-check" @click="onSave" />
      </div>
    </template>
  </BaseDrawer>
</template>

<style>
.lifearea-drawer-emoji {
  font-size: 1.5rem;
  line-height: 1;
}

.lifearea-drawer-colors {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.lifearea-color-chip {
  width: 1.5rem;
  height: 1.5rem;
}

.lifearea-drawer-emoji-input {
  width: 100%;
}

</style>
