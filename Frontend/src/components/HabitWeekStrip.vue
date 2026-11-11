<script setup lang="ts">
import type { HabitWithHistoryDTO } from '@/api/HabitsAPI'
import { useI18n } from 'vue-i18n'
import { toDateOnlyString } from '@/utils/dateOnly'
import { canMarkDay, isHabitScheduled, statusOn } from '@/utils/habitTracking'

defineProps<{
  item: HabitWithHistoryDTO
  days: Date[]
  today: string
  selectedDate?: string
  busy?: boolean
}>()
const emit = defineEmits<{ toggle: [date: Date] }>()
const { t, locale } = useI18n()
const format = (date: Date) =>
  date.toLocaleDateString(locale.value, { day: 'numeric', month: 'long' })
</script>

<template>
  <div class="habit-week-strip">
    <button
      v-for="day in days"
      :key="toDateOnlyString(day)"
      type="button"
      class="habit-day-button"
      :class="[
        statusOn(item, toDateOnlyString(day)),
        {
          'is-selected': toDateOnlyString(day) === selectedDate,
          'is-off': !isHabitScheduled(item.habit, toDateOnlyString(day)),
        },
      ]"
      :disabled="busy || !canMarkDay(item, toDateOnlyString(day), today)"
      :aria-label="`${item.habit.title}, ${format(day)}: ${t('habits.tracker.status.' + statusOn(item, toDateOnlyString(day)))}`"
      :aria-pressed="statusOn(item, toDateOnlyString(day)) === 'full'"
      @click="emit('toggle', day)"
    >
      <span aria-hidden="true">{{
        statusOn(item, toDateOnlyString(day)) === 'full'
          ? '✓'
          : statusOn(item, toDateOnlyString(day)) === 'skip'
            ? '−'
            : day.getDate()
      }}</span>
    </button>
  </div>
</template>
