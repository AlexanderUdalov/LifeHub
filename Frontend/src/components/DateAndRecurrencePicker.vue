<script setup lang="ts">
import DatePicker from 'primevue/datepicker'
import Select from 'primevue/select'
import Button from 'primevue/button'
import Divider from 'primevue/divider'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { RRule, rrulestr } from 'rrule'
import { presetToRule, ruleToPreset, type RecurrencePresetKey } from '@/composables/useRecurrencePresets'

const props = defineProps<{
  date: Date | null
  recurrenceRule: string | null
}>()

const emit = defineEmits<{
  (e: 'update:date', value: Date | null): void
  (e: 'update:recurrenceRule', value: string | null): void
  (e: 'close'): void
}>()

const { t, locale } = useI18n()

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
  { value: 'weekdays', labelKey: 'tasks.recurrence.preset.byWeekdays' }
]

const optionsWithLabels = computed(() => {
  const d = localDate.value ?? new Date()
  const weekday = d.getDay()
  const dayOfMonth = d.getDate()
  const month = d.getMonth()
  const year = d.getFullYear()
  const weekdayKey = String((weekday + 6) % 7)
  const monthShort = new Intl.DateTimeFormat(locale.value, { month: 'short' }).format(d)
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

const selectedWeekdays = ref<number[]>([])

const weekdayOptions = computed(() => [
  { value: 0, label: t('tasks.recurrence.weekdays.0') },
  { value: 1, label: t('tasks.recurrence.weekdays.1') },
  { value: 2, label: t('tasks.recurrence.weekdays.2') },
  { value: 3, label: t('tasks.recurrence.weekdays.3') },
  { value: 4, label: t('tasks.recurrence.weekdays.4') },
  { value: 5, label: t('tasks.recurrence.weekdays.5') },
  { value: 6, label: t('tasks.recurrence.weekdays.6') }
])

function normalizeWeekday(d: number | { weekday: number }): number {
  return typeof d === 'number' ? d : (d as { weekday: number }).weekday
}

function toWeekdayArray(byweekday: number | number[] | undefined): number[] {
  if (byweekday == null) return []
  const days = Array.isArray(byweekday) ? byweekday.map(normalizeWeekday) : [normalizeWeekday(byweekday)]
  return [...new Set(days)].sort((a, b) => a - b)
}

function toRulePart(rule: RRule): string | null {
  const match = rule.toString().match(/RRULE:(.+)/)
  return match?.[1]?.trim() ?? null
}

function parseRuleState(rruleStr: string | null | undefined, dt: Date | null | undefined) {
  const preset = ruleToPreset(rruleStr ?? null, dt ?? undefined)
  const base = {
    preset,
    weekdays: [] as number[]
  }
  if (!rruleStr?.trim()) return base

  try {
    const fullStr = rruleStr.toUpperCase().startsWith('RRULE:') ? rruleStr : `RRULE:${rruleStr}`
    const rule = rrulestr(fullStr, { dtstart: dt ?? new Date(), unfold: true })
    if (!(rule instanceof RRule)) return base
    const options = rule.options
    const weekdays = toWeekdayArray(options.byweekday)

    if (preset === 'weekdays') {
      return { ...base, weekdays: weekdays.length ? weekdays : [0] }
    }

    return base
  } catch {
    return base
  }
}

function syncStateFromProps() {
  const parsed = parseRuleState(props.recurrenceRule, props.date ? new Date(props.date) : null)
  selectedPreset.value = parsed.preset
  selectedWeekdays.value = parsed.weekdays
}

function emitWeekdaysRule() {
  const dt = localDate.value ?? new Date()
  const weekdays = [...new Set(selectedWeekdays.value)].sort((a, b) => a - b)
  if (weekdays.length === 0) {
    emit('update:recurrenceRule', null)
    return
  }
  const rule = new RRule({
    freq: RRule.WEEKLY,
    interval: 1,
    byweekday: weekdays,
    dtstart: dt
  })
  emit('update:recurrenceRule', toRulePart(rule))
}

watch(
  () => [props.recurrenceRule, props.date] as const,
  () => { syncStateFromProps() },
  { immediate: true }
)

const displayPreset = computed(() => selectedPreset.value)

function normalizePresetSelection(v: unknown): RecurrencePresetKey {
  if (v == null) return 'none'
  if (typeof v === 'object' && v !== null && 'value' in v) {
    return (v as { value: RecurrencePresetKey }).value
  }
  return v as RecurrencePresetKey
}

function applyPreset(preset: RecurrencePresetKey) {
  selectedPreset.value = preset
  if (preset === 'weekdays') {
    if (selectedWeekdays.value.length === 0) {
      const wd = ((localDate.value ?? new Date()).getDay() + 6) % 7
      selectedWeekdays.value = [wd]
    }
    emitWeekdaysRule()
    return
  }
  const dt = localDate.value ?? new Date()
  const rule = presetToRule(preset, dt)
  emit('update:recurrenceRule', rule)
}

function toggleWeekday(value: number) {
  const current = selectedWeekdays.value
  selectedWeekdays.value = current.includes(value)
    ? current.filter((v) => v !== value)
    : [...current, value].sort((a, b) => a - b)
  emitWeekdaysRule()
}

function isWeekdaySelected(value: number) {
  return selectedWeekdays.value.includes(value)
}

function onDateSelect(value: Date | Date[] | (Date | null)[] | null | undefined) {
  if (value == null) {
    setDate(null)
    return
  }
  const d = Array.isArray(value) ? value[0] ?? null : value
  const date = d ? new Date(d) : null
  setDate(date)
  if (selectedPreset.value === 'weekdays') {
    emitWeekdaysRule()
    return
  }
  if (date && selectedPreset.value !== 'none' && selectedPreset.value !== 'daily') {
    const rule = presetToRule(selectedPreset.value, date)
    emit('update:recurrenceRule', rule)
  }
}

function onClear() {
  setDate(null)
  selectedPreset.value = 'none'
  emit('update:recurrenceRule', null)
  emit('close')
}

function onConfirm() {
  emit('close')
}
</script>

<template>
  <div class="date-and-recurrence-picker">
    <DatePicker :model-value="localDate" @update:model-value="onDateSelect" date-format="dd.mm.yy" inline
      class="date-and-recurrence-calendar" />
    <Divider />
    <div class="repeat-section">
      <label class="repeat-label">{{ t('tasks.recurrence.repeat') }}</label>
      <Select :model-value="displayPreset" :options="optionsWithLabels" option-value="value" option-label="label"
        class="repeat-select" @update:model-value="(v: unknown) => applyPreset(normalizePresetSelection(v))" />

      <div v-if="selectedPreset === 'weekdays'" class="weekday-picker">
        <button v-for="wd in weekdayOptions" :key="wd.value" type="button" class="weekday-btn"
          :class="{ selected: isWeekdaySelected(wd.value) }" :aria-pressed="isWeekdaySelected(wd.value)"
          @click="toggleWeekday(wd.value)">
          {{ wd.label }}
        </button>
      </div>

    </div>
    <Divider />
    <div class="picker-actions">
      <Button :label="t('tasks.recurrence.clear')" severity="secondary" text @click="onClear" />
      <Button :label="t('tasks.recurrence.confirm')" @click="onConfirm" />
    </div>
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
}

.repeat-label {
  font-size: 0.875rem;
  color: var(--p-text-muted-color);
}

.repeat-select {
  width: 100%;
}

.weekday-picker {
  display: flex;
  width: 100%;
  border: 1px solid var(--p-content-border-color);
  border-radius: var(--p-border-radius);
  overflow: hidden;
}

.weekday-btn {
  flex: 1;
  padding: 0.5rem 0;
  font-size: 0.8125rem;
  font-weight: 500;
  text-align: center;
  border: none;
  border-right: 1px solid var(--p-content-border-color);
  background: var(--p-content-background);
  color: var(--p-text-color);
  cursor: pointer;
  transition: background-color 0.15s, color 0.15s;
}

.weekday-btn:last-child {
  border-right: none;
}

.weekday-btn:hover {
  background: var(--p-content-hover-background);
}

.weekday-btn.selected {
  background: var(--p-primary-color);
  color: var(--p-primary-contrast-color);
}

.picker-actions {
  display: flex;
  justify-content: space-between;
  gap: 0.5rem;
}
</style>
