<script setup lang="ts">
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import Message from 'primevue/message'
import SelectButton from 'primevue/selectbutton'
import InputNumber from 'primevue/inputnumber'
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { RRule } from 'rrule'

import { useHabitsStore } from '@/stores/habits'
import type { HabitDTO } from '@/api/HabitsAPI'
import { parseRuleToOptions, optionsToRuleString } from '@/composables/useRecurrenceOptions'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { useGoalsStore } from '@/stores/goals'
import { useApiError } from '@/composables/useApiError'
import BaseDrawer from '@/components/base/BaseDrawer.vue'

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

const visible = ref(true)
watch(visible, (v) => {
  if (!v) emit('close')
})

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

const lifeAreaChipLabel = computed(() => {
  if (!localHabit.value.lifeAreaId) return t('lifeareas.field')
  const area = lifeAreasStore.lifeAreas.find(a => a.id === localHabit.value.lifeAreaId)
  return area?.name ?? t('lifeareas.field')
})

const hasLifeArea = computed(() => !!localHabit.value.lifeAreaId)

const goalChipLabel = computed(() => {
  if (!localHabit.value.goalId) return t('goals.field')
  const goal = goalsStore.goalsSorted.find(g => g.id === localHabit.value.goalId)
  return goal?.title ?? t('goals.field')
})

const hasGoal = computed(() => !!localHabit.value.goalId)

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
  <BaseDrawer v-model:visible="visible" class="habit-drawer">
    <template #header>
      <div class="ds-drawer-title-row" ref="titleWrap">
        <InputText v-model="localHabit.title" :placeholder="t('habits.editdialog.newHabit')"
          class="ds-drawer-title-input" />
      </div>
    </template>

    <div class="ds-drawer-section">
      <label class="ds-drawer-label">{{ t('habits.editdialog.color') }}</label>
      <div class="habit-drawer-colors">
        <button v-for="color in HABIT_COLOR_OPTIONS" :key="color" type="button" class="ds-color-swatch"
          :class="{ 'is-selected': localHabit.color === color }" :style="{ backgroundColor: color }"
          :aria-pressed="localHabit.color === color" :aria-label="color" @click="localHabit.color = color" />
      </div>
    </div>

    <div class="ds-drawer-section">
      <label class="ds-drawer-label">{{ t('habits.editdialog.days') }}</label>
      <SelectButton v-model="habitMode" :options="habitModeOptions" option-label="label" option-value="value"
        :allow-empty="false" class="habit-drawer-mode-select" />

      <template v-if="localHabit.timesPerWeekGoal == null">
        <div class="habit-drawer-weekdays">
          <button v-for="wd in weekdayOptions" :key="wd.value" type="button" class="habit-wd-btn"
            :class="{ selected: isWeekdaySelected(wd.value) }" :aria-pressed="isWeekdaySelected(wd.value)"
            @click="toggleWeekday(wd.value)">
            {{ wd.label }}
          </button>
        </div>
      </template>

      <template v-else>
        <div class="habit-drawer-times-row">
          <span class="habit-drawer-times-label">{{ t('habits.editdialog.timesPerWeekLabel') }}</span>
          <InputNumber v-model="localHabit.timesPerWeekGoal" buttonLayout="horizontal"
            :min="1" :max="7" show-buttons class="habit-drawer-times-input" />
        </div>
      </template>
    </div>

    <div class="ds-chip-row">
      <Select v-model="localHabit.lifeAreaId" :options="lifeAreasStore.lifeAreas" option-label="name" option-value="id"
        show-clear :placeholder="t('lifeareas.field')" class="ds-chip-select"
        :class="{ 'ds-chip-select--active': hasLifeArea }">
        <template #value>
          <span class="ds-chip-select-value">
            <i class="pi pi-objects-column" />
            {{ lifeAreaChipLabel }}
          </span>
        </template>
      </Select>

      <Select v-model="localHabit.goalId" :options="goalsStore.goalsSorted" option-label="title" option-value="id"
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

    <Message v-if="errorText.length" severity="error" icon="pi pi-times-circle" :life="3000">
      {{ errorText }}
    </Message>

    <template #footer>
      <div class="ds-drawer-actions">
        <Button v-if="isEdit" icon="pi pi-trash" :label="t('habits.editdialog.delete')" severity="danger" variant="text"
          size="small" :loading="isDeleteLoading" @click="onDelete" />
        <span v-else />
        <Button :label="isEdit ? t('habits.editdialog.save') : t('habits.editdialog.create')" :disabled="!canSave"
          :loading="isSaveLoading" icon="pi pi-check" @click="onSave" />
      </div>
    </template>
  </BaseDrawer>
</template>

<style>
.habit-drawer-colors {
  display: flex;
  justify-content: space-between;
}

.habit-drawer-mode-select {
  width: 100%;
}

.habit-drawer-mode-select .p-selectbutton-option {
  flex: 1;
  justify-content: center;
  font-size: 0.8125rem;
}

.habit-drawer-weekdays {
  display: flex;
  width: 100%;
  border: 1px solid var(--p-content-border-color);
  border-radius: var(--p-border-radius);
  overflow: hidden;
}

.habit-wd-btn {
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

.habit-wd-btn:last-child {
  border-right: none;
}

.habit-wd-btn:hover {
  background: var(--p-content-hover-background);
}

.habit-wd-btn.selected {
  background: var(--p-primary-color);
  color: var(--p-primary-contrast-color);
}

.habit-drawer-hint {
  font-size: 0.8rem;
  color: var(--p-text-muted-color);
}

.habit-drawer-times-row {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.habit-drawer-times-label {
  flex: 1;
  min-width: 0;
  font-size: 0.875rem;
  color: var(--p-text-muted-color);
}

.habit-drawer-times-input {
  flex-shrink: 0;
}

.habit-drawer-times-input .p-inputnumber-input {
  text-align: center;
  width: 2.5rem;
  padding: 0.4rem 0;
}

</style>
