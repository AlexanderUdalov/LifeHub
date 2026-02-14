<script setup lang="ts">
import DatePicker from 'primevue/datepicker'
import Select from 'primevue/select'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { presetToRule, ruleToPreset, type RecurrencePresetKey } from '@/composables/useRecurrencePresets'

const props = defineProps<{
  date: Date | null
  recurrenceRule: string | null
}>()

const emit = defineEmits<{
  (e: 'update:date', value: Date | null): void
  (e: 'update:recurrenceRule', value: string | null): void
}>()

const { t } = useI18n()

const localDate = ref<Date | null>(props.date ?? null)
watch(
  () => props.date,
  (d) => { localDate.value = d ? new Date(d) : null },
  { immediate: true }
)

function setDate(value: Date | null) {
  localDate.value = value
  emit('update:date', value ?? null)
}

const presetOptions: { value: RecurrencePresetKey; labelKey: string; labelArg?: Record<string, string | number> }[] = [
  { value: 'none', labelKey: 'tasks.recurrence.preset.none' },
  { value: 'daily', labelKey: 'tasks.recurrence.preset.daily' },
  { value: 'weekly', labelKey: 'tasks.recurrence.preset.weekly', labelArg: {} },
  { value: 'monthly', labelKey: 'tasks.recurrence.preset.monthly', labelArg: {} },
  { value: 'yearly', labelKey: 'tasks.recurrence.preset.yearly', labelArg: {} },
  { value: 'weekdays', labelKey: 'tasks.recurrence.preset.weekdays' }
  // 'custom' — скрыт, задел на будущее
]

const optionsWithLabels = computed(() => {
  const d = localDate.value ?? new Date()
  const weekday = d.getDay()
  const dayOfMonth = d.getDate()
  const month = d.getMonth()
  const year = d.getFullYear()
  const weekdayKey = String((weekday + 6) % 7)
  const monthShort = d.toLocaleDateString(undefined, { month: 'short' })
  return presetOptions.map((opt) => {
    if (opt.value === 'weekly') {
      return { value: opt.value, label: t('tasks.recurrence.preset.weeklyWithDay', { weekday: t(`tasks.recurrence.weekdays.${weekdayKey}`) }) }
    }
    if (opt.value === 'monthly') {
      return { value: opt.value, label: t('tasks.recurrence.preset.monthlyWithDay', { day: dayOfMonth }) }
    }
    if (opt.value === 'yearly') {
      return { value: opt.value, label: t('tasks.recurrence.preset.yearlyWithDate', { day: dayOfMonth, month: monthShort, year }) }
    }
    return { value: opt.value, label: t(opt.labelKey) }
  })
})

const selectedPreset = ref<RecurrencePresetKey>(
  ruleToPreset(props.recurrenceRule ?? null, props.date ? new Date(props.date) : undefined)
)

watch(
  () => [props.recurrenceRule, props.date] as const,
  ([rule, date]) => {
    selectedPreset.value = ruleToPreset(rule ?? null, date ? new Date(date) : undefined)
  },
  { immediate: false }
)

/** Для селекта показываем 'none' вместо 'custom', т.к. пункт «Произвольно» скрыт */
const displayPreset = computed(() =>
  selectedPreset.value === 'custom' ? 'none' : selectedPreset.value
)

function applyPreset(preset: RecurrencePresetKey) {
  if (preset === 'custom') return
  selectedPreset.value = preset
  const dt = localDate.value ?? new Date()
  const rule = presetToRule(preset, dt)
  emit('update:recurrenceRule', rule)
}

function onDateSelect(value: Date | Date[] | (Date | null)[] | null | undefined) {
  if (value == null) {
    setDate(null)
    return
  }
  const d = Array.isArray(value) ? value[0] ?? null : value
  const date = d ? new Date(d) : null
  setDate(date)
  if (date && selectedPreset.value !== 'none' && selectedPreset.value !== 'daily' && selectedPreset.value !== 'weekdays') {
    const rule = presetToRule(selectedPreset.value, date)
    emit('update:recurrenceRule', rule)
  }
}
</script>

<template>
  <div class="date-and-recurrence-picker">
    <DatePicker
      :model-value="localDate"
      @update:model-value="onDateSelect"
      date-format="dd.mm.yy"
      inline
      class="date-and-recurrence-calendar"
    >
      <template #footer>
        <div class="repeat-section">
          <label class="repeat-label">{{ t('tasks.recurrence.repeat') }}</label>
          <Select
            :model-value="displayPreset"
            :options="optionsWithLabels"
            option-value="value"
            option-label="label"
            class="repeat-select"
            @update:model-value="(v: RecurrencePresetKey) => applyPreset(v)"
          />
        </div>
      </template>
    </DatePicker>
  </div>
</template>

<style scoped>
.date-and-recurrence-picker {
  width: 100%;
}

.date-and-recurrence-calendar {
  width: 100%;
}

.repeat-section {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  padding-top: 0.5rem;
  margin-top: 0.5rem;
}

.repeat-label {
  font-size: 0.875rem;
  color: var(--p-text-muted-color);
}

.repeat-select {
  width: 100%;
}
</style>
