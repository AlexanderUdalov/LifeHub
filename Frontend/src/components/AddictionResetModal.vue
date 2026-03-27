<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import Button from 'primevue/button'
import Textarea from 'primevue/textarea'
import DatePicker from 'primevue/datepicker'
import type { AddictionDTO } from '@/api/AddictionsAPI'
import { useI18n } from 'vue-i18n'
import BaseDrawer from '@/components/base/BaseDrawer.vue'

const props = defineProps<{
  addiction: AddictionDTO
  prefilledDate?: Date | null
}>()

const emit = defineEmits<{
  (e: 'confirm', payload: { addiction: AddictionDTO; resetAt: Date; note: string }): void
  (e: 'close'): void
}>()

const { t, locale } = useI18n()

const visible = ref(true)
const note = ref('')
/** Combined local date+time for the relapse moment */
const resetAt = ref(new Date())

watch(visible, (v) => {
  if (!v) emit('close')
})

watch(
  () => props.prefilledDate,
  (d) => {
    const base = new Date()
    if (d) {
      resetAt.value = new Date(
        d.getFullYear(),
        d.getMonth(),
        d.getDate(),
        base.getHours(),
        base.getMinutes(),
        0,
        0
      )
    } else {
      resetAt.value = base
    }
  },
  { immediate: true }
)

const prefilledDateLabel = computed(() => {
  if (!props.prefilledDate) return ''
  const loc = locale.value === 'ru' ? 'ru-RU' : 'en-US'
  return props.prefilledDate.toLocaleDateString(loc, {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric'
  })
})

function confirmReset() {
  emit('confirm', {
    addiction: props.addiction,
    resetAt: resetAt.value,
    note: note.value
  })
  note.value = ''
}
</script>

<template>
  <BaseDrawer
    v-model:visible="visible"
    class="addiction-drawer"
  >
    <template #header>
      <div class="reset-drawer-title-row">
        <i class="pi pi-undo reset-title-icon" />
        <span>{{ t('addictions.resetDrawer.title') }}</span>
      </div>
    </template>

    <p class="reset-addiction-name">
      {{ addiction.title }}
    </p>

    <div v-if="prefilledDate" class="addiction-drawer-section">
      <label class="addiction-drawer-label">{{ t('addictions.resetDrawer.date') }}</label>
      <p class="reset-prefilled-date">{{ prefilledDateLabel }}</p>
    </div>

    <div class="addiction-drawer-section">
      <label class="addiction-drawer-label">
        {{ prefilledDate ? t('addictions.resetDrawer.time') : t('addictions.resetDrawer.dateTime') }}
      </label>
      <DatePicker
        v-if="!prefilledDate"
        v-model="resetAt"
        showTime
        hourFormat="24"
        date-format="dd.mm.yy"
        fluid
        show-button-bar
      />
      <DatePicker v-else v-model="resetAt" timeOnly hourFormat="24" fluid />
    </div>

    <div class="addiction-drawer-section reset-note-section">
      <label class="addiction-drawer-label">{{ t('addictions.resetDrawer.note') }}</label>
      <Textarea
        v-model="note"
        class="reset-note-textarea w-full"
        :placeholder="t('addictions.resetDrawer.notePlaceholder')"
        autoResize
        rows="6"
      />
    </div>

    <template #footer>
      <div class="addiction-drawer-actions reset-actions">
        <Button :label="t('cancel')" severity="secondary" text @click="visible = false" />
        <Button
          :label="t('addictions.resetDrawer.confirm')"
          severity="danger"
          icon="pi pi-undo"
          @click="confirmReset"
        />
      </div>
    </template>
  </BaseDrawer>
</template>

<style>
.reset-drawer-title-row {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  font-size: 1.1rem;
}

.reset-title-icon {
  color: var(--red-500);
}

.reset-addiction-name {
  font-size: 1rem;
  font-weight: 500;
  margin: 0 0 4px;
  color: var(--p-text-color);
}

.reset-prefilled-date {
  margin: 0;
  font-size: 1rem;
  font-weight: 500;
}

.reset-note-section {
  width: 100%;
}

.reset-note-textarea {
  width: 100% !important;
  min-height: 10rem;
  box-sizing: border-box;
}

.reset-note-textarea.p-textarea {
  width: 100%;
}

.reset-actions {
  justify-content: flex-end;
}
</style>
