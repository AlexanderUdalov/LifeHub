<script setup lang="ts">
import Dialog from 'primevue/dialog'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import Message from 'primevue/message'
import ListBox from 'primevue/listbox'
import InputNumber from 'primevue/inputnumber'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { RRule } from 'rrule'

import { useHabitsStore } from '@/stores/habits'
import type { HabitDTO } from '@/api/HabitsAPI'
import { parseRuleToOptions, optionsToRuleString } from '@/composables/useRecurrenceOptions'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { useGoalsStore } from '@/stores/goals'
import { useApiError } from '@/composables/useApiError'

const HABIT_COLOR_OPTIONS = [
  '#3b82f6', '#ef4444', '#22c55e', '#eab308', '#a855f7', '#ec4899', '#06b6d4'
]

type LocalHabit = {
  title: string
  color: string
  selectedWeekdays: number[]
  goalId: string | null
  lifeAreaId: string | null
  timesPerWeekGoal: number | null
}

function getInitialHabit(habit: HabitDTO | null): LocalHabit {
  if (!habit) {
    return {
      title: '',
      color: HABIT_COLOR_OPTIONS[0] ?? '#3b82f6',
      selectedWeekdays: [],
      goalId: null,
      lifeAreaId: null,
      timesPerWeekGoal: null
    }
  }
  const goal = habit.timesPerWeekGoal
  if (typeof goal === 'number' && goal >= 1 && goal <= 7) {
    return {
      title: habit.title,
      color: habit.color,
      selectedWeekdays: [],
      goalId: habit.goalId,
      lifeAreaId: habit.lifeAreaId,
      timesPerWeekGoal: goal
    }
  }
  const opt = parseRuleToOptions(habit.recurrenceRule)
  const selectedWeekdays = opt.byweekday ?? []
  return {
    title: habit.title,
    color: habit.color,
    selectedWeekdays,
    goalId: habit.goalId,
    lifeAreaId: habit.lifeAreaId,
    timesPerWeekGoal: null
  }
}

const props = defineProps<{
  habit: HabitDTO | null
}>()

const emit = defineEmits<{
  (e: 'close'): void
}>()

const { t } = useI18n()
const habitsStore = useHabitsStore()
const lifeAreasStore = useLifeAreasStore()
const goalsStore = useGoalsStore()
const apiError = useApiError()

const localHabit = ref<LocalHabit>(getInitialHabit(props.habit))

const isEdit = computed(() => !!props.habit)
const canSave = computed(() => {
  if (localHabit.value.title.trim().length === 0 || localHabit.value.color.trim().length === 0) return false
  const n = localHabit.value.timesPerWeekGoal
  if (n != null) return n >= 1 && n <= 7
  return true
})

const weekdayOptions = computed(() => [
  { value: 0, label: t('tasks.recurrence.weekdays.0') },
  { value: 1, label: t('tasks.recurrence.weekdays.1') },
  { value: 2, label: t('tasks.recurrence.weekdays.2') },
  { value: 3, label: t('tasks.recurrence.weekdays.3') },
  { value: 4, label: t('tasks.recurrence.weekdays.4') },
  { value: 5, label: t('tasks.recurrence.weekdays.5') },
  { value: 6, label: t('tasks.recurrence.weekdays.6') }
])

function toggleWeekday(value: number) {
  const arr = localHabit.value.selectedWeekdays
  const idx = arr.indexOf(value)
  if (idx === -1) {
    localHabit.value.selectedWeekdays = [...arr, value].sort((a, b) => a - b)
  } else {
    localHabit.value.selectedWeekdays = arr.filter((v) => v !== value)
  }
}

function isWeekdaySelected(value: number) {
  return localHabit.value.selectedWeekdays.includes(value)
}

const habitModeOptions = computed(() => [
  { label: t('habits.editdialog.modeWeekdays'), value: 'weekdays' as const },
  { label: t('habits.editdialog.modeTimesPerWeek'), value: 'timesPerWeek' as const }
])

const habitMode = computed({
  get: () => (localHabit.value.timesPerWeekGoal == null ? 'weekdays' : 'timesPerWeek'),
  set: (v: 'weekdays' | 'timesPerWeek') => {
    if (v === 'weekdays') localHabit.value.timesPerWeekGoal = null
    else localHabit.value.timesPerWeekGoal = 3
  }
})

const recurrenceRule = computed<string>(() => {
  if (localHabit.value.timesPerWeekGoal != null) return 'FREQ=WEEKLY'
  const days = [...new Set(localHabit.value.selectedWeekdays)].sort((a, b) => a - b)
  if (days.length === 0 || days.length === 7) return 'FREQ=DAILY'

  return (
    optionsToRuleString(
      {
        freq: RRule.WEEKLY,
        interval: 1,
        byweekday: days,
        bymonthday: []
      },
      new Date()
    ) ?? 'FREQ=DAILY'
  )
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
      title: localHabit.value.title.trim(),
      color: localHabit.value.color.trim(),
      recurrenceRule: recurrenceRule.value,
      timesPerWeekGoal: localHabit.value.timesPerWeekGoal,
      goalId: localHabit.value.goalId,
      lifeAreaId: localHabit.value.lifeAreaId
    }

    if (isEdit.value) {
      await habitsStore.updateHabit(props.habit!.id, request)
    } else {
      await habitsStore.createHabit(request)
    }

    emit('close')
  } catch (e) {
    errorText.value = apiError.resolveMessage(e)
  } finally {
    isSaveLoading.value = false
  }
}

async function onDelete() {
  if (!props.habit) return
  errorText.value = ''
  isDeleteLoading.value = true
  try {
    await habitsStore.deleteHabit(props.habit.id)
    emit('close')
  } catch (e) {
    errorText.value = apiError.resolveMessage(e)
  } finally {
    isDeleteLoading.value = false
  }
}

</script>

<template>
  <Dialog modal :visible="true" :closable="false" :draggable="false" @hide="emit('close')">
    <template #header>
      <div class="habit-edit-header">
        <InputText id="habit-title" v-model="localHabit.title" class="habit-edit-title"
          :placeholder="isEdit ? '' : t('habits.editdialog.newHabit')" size="large" />
        <Button icon="pi pi-times" severity="secondary" rounded variant="outlined" aria-label="Cancel" size="small"
          @click="emit('close')" />
      </div>
    </template>

    <div class="form-field">
      <label class="field-label">{{ t('habits.editdialog.color') }}</label>
      <div class="color-chips">
        <button v-for="color in HABIT_COLOR_OPTIONS" :key="color" type="button" class="color-chip"
          :class="{ selected: localHabit.color === color }" :style="{ backgroundColor: color }"
          :aria-pressed="localHabit.color === color" :aria-label="color" @click="localHabit.color = color" />
      </div>
    </div>

    <div class="form-field">
      <label class="field-label">{{ t('goals.field') }}</label>
      <Select v-model="localHabit.goalId" :options="goalsStore.goalsSorted" option-label="title" option-value="id" show-clear
        :placeholder="t('goals.selectPlaceholder')" />
    </div>

    <div class="form-field">
      <label class="field-label">{{ t('lifeareas.field') }}</label>
      <Select v-model="localHabit.lifeAreaId" :options="lifeAreasStore.lifeAreas" option-label="name" option-value="id"
        show-clear :placeholder="t('lifeareas.selectPlaceholder')" />
    </div>

    <div class="form-field">
      <label class="field-label">{{ t('habits.editdialog.days') }}</label>
      <ListBox v-model="habitMode" :options="habitModeOptions" option-label="label" option-value="value"
        class="habit-mode-listbox" />

      <template v-if="localHabit.timesPerWeekGoal == null">
        <div class="weekday-pills">
          <button v-for="wd in weekdayOptions" :key="wd.value" type="button" class="weekday-pill"
            :class="{ selected: isWeekdaySelected(wd.value) }" :aria-pressed="isWeekdaySelected(wd.value)"
            @click="toggleWeekday(wd.value)">
            {{ wd.label }}
          </button>
        </div>
        <div class="hint">{{ localHabit.selectedWeekdays.length && localHabit.selectedWeekdays.length < 7 ?
          t('habits.editdialog.weekly') : t('habits.editdialog.everyDay') }}</div>
      </template>

      <template v-else>
        <div class="times-per-week-row">
          <label class="times-per-week-label" for="habit-times-per-week">
            {{ t('habits.editdialog.timesPerWeekLabel') }}
          </label>
          <InputNumber id="habit-times-per-week" v-model="localHabit.timesPerWeekGoal" buttonLayout="vertical" :min="1"
            :max="7" show-buttons class="times-per-week-input" />
        </div>
      </template>
    </div>

    <Message v-if="errorText.length" severity="error" icon="pi pi-times-circle" :life="3000">
      {{ errorText }}
    </Message>

    <template #footer>
      <Button v-if="isEdit" :label="t('habits.editdialog.delete')" severity="danger" @click="onDelete"
        :loading="isDeleteLoading" />
      <Button :label="isEdit ? t('habits.editdialog.save') : t('habits.editdialog.create')" :disabled="!canSave"
        @click="onSave" :loading="isSaveLoading" />
    </template>
  </Dialog>
</template>

<style scoped>
.habit-edit-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  width: 100%;
}

.habit-edit-title {
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

.habit-mode-listbox {
  width: 100%;
}

.times-per-week-row {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.times-per-week-label {
  flex: 1;
  min-width: 0;
  font-size: 0.875rem;
  color: var(--p-text-muted-color);
}

.times-per-week-input {
  width: 3rem;
}

.weekday-pills {
  display: flex;
  flex-wrap: wrap;
  justify-content: space-between;
  gap: 0.35rem;
  width: 100%;
}

.weekday-pill {
  min-width: 2.25rem;
  padding: 0.35rem 0.5rem;
  font-size: 0.8125rem;
  border-radius: var(--p-border-radius);
  border: 1px solid var(--p-content-border-color);
  background: var(--p-content-background);
  color: var(--p-text-color);
  cursor: pointer;
  transition: background-color 0.15s, border-color 0.15s;
}

.weekday-pill:hover {
  background: var(--p-content-hover-background);
  border-color: var(--p-content-hover-border-color);
}

.weekday-pill.selected {
  background: var(--p-primary-color);
  border-color: var(--p-primary-color);
  color: var(--p-primary-contrast-color);
}

.hint {
  font-size: 0.8rem;
  color: var(--p-text-muted-color);
}
</style>