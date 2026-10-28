<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import Button from 'primevue/button'
import DatePicker from 'primevue/datepicker'
import Message from 'primevue/message'
import Textarea from 'primevue/textarea'
import BaseCard from '@/components/base/BaseCard.vue'
import { getDayFeedback, saveDayFeedback, type DayFeedbackDTO } from '@/api/DayFeedbackAPI'
import { toDateOnlyString } from '@/utils/dateOnly'

const { t } = useI18n()
const yesterday = new Date()
yesterday.setDate(yesterday.getDate() - 1)
const selectedDate = ref<Date | null>(yesterday)
const dateKey = computed(() => selectedDate.value ? toDateOnlyString(selectedDate.value) : '')
const score = ref(5)
const note = ref('')
const saved = ref<DayFeedbackDTO | null>(null)
const loading = ref(false)
const saving = ref(false)
const loadFailed = ref(false)
const error = ref('')
let loadVersion = 0

const emoji = computed(() => ['😭', '😞', '😔', '🙁', '😕', '😐', '🙂', '😊', '😄', '🤩', '🥳'][score.value])
const unchanged = computed(() => saved.value?.score === score.value && (saved.value?.note ?? '') === note.value.trim())
const disabled = computed(() => loading.value || saving.value || loadFailed.value || !dateKey.value)

async function loadFeedback() {
  const version = ++loadVersion
  const date = dateKey.value
  error.value = ''
  loadFailed.value = false
  saved.value = null
  score.value = 5
  note.value = ''
  loading.value = !!date
  if (!date) return
  try {
    const result = await getDayFeedback(date)
    if (version !== loadVersion) return
    saved.value = result
    score.value = Number(result?.score ?? 5)
    note.value = result?.note ?? ''
  } catch {
    if (version !== loadVersion) return
    loadFailed.value = true
    error.value = t('dayFeedback.loadError')
  } finally {
    if (version === loadVersion) loading.value = false
  }
}

watch(dateKey, loadFeedback, { immediate: true })

async function save() {
  if (disabled.value || unchanged.value) return
  saving.value = true
  error.value = ''
  try {
    saved.value = await saveDayFeedback(dateKey.value, { score: score.value, note: note.value.trim() || null })
    note.value = saved.value.note ?? ''
  } catch {
    error.value = t('dayFeedback.saveError')
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <BaseCard class="day-feedback">
    <template #content>
      <form class="day-feedback__form" :aria-busy="loading || saving" @submit.prevent="save">
        <div class="day-feedback__header">
          <h2 class="day-feedback__title">{{ t('dayFeedback.title') }}</h2>
          <DatePicker v-model="selectedDate" :max-date="new Date()" :disabled="saving" date-format="dd.mm.yy"
            :manual-input="false" show-icon input-id="day-feedback-date" :aria-label="t('dayFeedback.date')"
            class="day-feedback__date" />
        </div>
        <p class="day-feedback__hint">{{ t('dayFeedback.subtitle') }}</p>

        <div class="day-feedback__rating">
          <span class="day-feedback__emoji" aria-hidden="true">{{ emoji }}</span>
          <output for="day-feedback-score" class="day-feedback__value">{{ score }}<span> / 10</span></output>
        </div>
        <label for="day-feedback-score" class="day-feedback__label">{{ t('dayFeedback.score') }}</label>
        <input id="day-feedback-score" v-model.number="score" type="range" min="0" max="10" step="1"
          :disabled="disabled" :aria-valuetext="`${score} / 10`" class="day-feedback__slider" />
        <div class="day-feedback__scale">
          <span>{{ t('dayFeedback.worst') }}</span>
          <span>{{ t('dayFeedback.best') }}</span>
        </div>

        <label for="day-feedback-note" class="day-feedback__label">{{ t('dayFeedback.note') }}</label>
        <Textarea id="day-feedback-note" v-model="note" :disabled="disabled" :maxlength="1000" :rows="2"
          :placeholder="t('dayFeedback.notePlaceholder')" auto-resize fluid />
        <small class="day-feedback__hint day-feedback__counter">{{ note.length }} / 1000</small>

        <Message v-if="error" severity="error" :closable="false">
          {{ error }}
          <Button v-if="loadFailed" :label="t('dayFeedback.retry')" variant="text" size="small" @click="loadFeedback" />
        </Message>
        <div class="day-feedback__footer">
          <span class="day-feedback__hint" role="status">{{ loading ? t('dayFeedback.loading') : unchanged ? t('dayFeedback.saved') : '' }}</span>
          <Button type="submit" :label="t('dayFeedback.save')" icon="pi pi-check" size="small"
            :disabled="disabled || unchanged" :loading="saving" />
        </div>
      </form>
    </template>
  </BaseCard>
</template>

<style scoped>
.day-feedback {
  margin: var(--ds-space-2) 0 var(--ds-space-5);
}

.day-feedback__form {
  display: flex;
  flex-direction: column;
  gap: var(--ds-space-2);
}

.day-feedback__header,
.day-feedback__footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: var(--ds-space-3);
}

.day-feedback__title {
  margin: 0;
  font-size: var(--ds-font-size-lg);
  font-weight: 600;
}

.day-feedback__date {
  width: 10rem;
  max-width: 100%;
}

.day-feedback__date :deep(input) {
  width: 100%;
  min-width: 0;
}

.day-feedback__hint,
.day-feedback__scale {
  margin: 0;
  color: var(--p-text-muted-color);
  font-size: var(--ds-font-size-xs);
}

.day-feedback__rating {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--ds-space-3);
  padding: var(--ds-space-2) 0;
}

.day-feedback__emoji {
  font-size: 2.5rem;
}

.day-feedback__value {
  font-size: 2rem;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
}

.day-feedback__value span {
  font-size: var(--ds-font-size-md);
  color: var(--p-text-muted-color);
  font-weight: 400;
}

.day-feedback__label {
  font-size: var(--ds-font-size-sm);
  font-weight: 500;
}

.day-feedback__slider {
  width: 100%;
  min-height: 1.75rem;
  margin: 0;
  cursor: pointer;
  accent-color: var(--p-primary-color);
}

.day-feedback__slider:disabled {
  cursor: default;
  opacity: 0.5;
}

.day-feedback__scale {
  display: flex;
  justify-content: space-between;
  gap: var(--ds-space-3);
  margin-bottom: var(--ds-space-3);
}

.day-feedback__scale span {
  max-width: 48%;
}

.day-feedback__scale span:last-child {
  text-align: right;
}

.day-feedback__counter {
  text-align: right;
}
</style>
