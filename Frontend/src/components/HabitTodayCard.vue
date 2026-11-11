<script setup lang="ts">
import type { HabitWithHistoryDTO } from '@/api/HabitsAPI'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { statusOn, scheduleOn, weekCompletions } from '@/utils/habitTracking'
import HabitWeekStrip from './HabitWeekStrip.vue'

const props = defineProps<{
  item: HabitWithHistoryDTO
  date: string
  today: string
  days: Date[]
  busy: boolean
}>()
const emit = defineEmits<{ open: []; toggle: [date: Date] }>()
const { t } = useI18n()
const areas = useLifeAreasStore()
const status = computed(() => statusOn(props.item, props.date))
const goal = computed(() => scheduleOn(props.item.habit, props.date).goal)
const weeklyCount = computed(() => weekCompletions(props.item, props.date))
const subtitle = computed(() =>
  goal.value
    ? t(
        weeklyCount.value >= goal.value
          ? 'habits.tracker.weekGoalMet'
          : 'habits.tracker.weekProgress',
        { count: weeklyCount.value, goal: goal.value },
      )
    : status.value !== 'none'
      ? t(`habits.tracker.status.${status.value}`)
      : (areas.lifeAreas.find((a) => a.id === props.item.habit.lifeAreaId)?.name ??
        t('habits.tracker.ready')),
)
</script>

<template>
  <article class="habit-entry" :aria-label="item.habit.title">
    <div class="habit-entry-top">
      <button type="button" class="habit-name-button" @click="emit('open')">
        <strong>{{ item.habit.title }}</strong>
        <small>{{ subtitle }}</small>
      </button>
      <button
        type="button"
        class="habit-complete-button"
        :class="status"
        :disabled="busy"
        :aria-pressed="status === 'full'"
        :aria-label="
          t(status === 'full' ? 'habits.tracker.undoHabit' : 'habits.tracker.completeHabit', {
            title: item.habit.title,
          })
        "
        @click="emit('toggle', new Date(date + 'T00:00:00'))"
      >
        <i
          :class="
            busy ? 'pi pi-spin pi-spinner' : status === 'skip' ? 'pi pi-minus' : 'pi pi-check'
          "
          aria-hidden="true"
        />
      </button>
    </div>
    <HabitWeekStrip
      :item="item"
      :days="days"
      :today="today"
      :selected-date="date"
      :busy="busy"
      @toggle="emit('toggle', $event)"
    />
  </article>
</template>
