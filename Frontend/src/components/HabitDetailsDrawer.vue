<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import Button from 'primevue/button'
import Message from 'primevue/message'
import BaseDrawer from '@/components/base/BaseDrawer.vue'
import { useHabitsStore } from '@/stores/habits'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { useGoalsStore } from '@/stores/goals'
import type { HabitDTO, HabitCompletion, HabitState } from '@/api/HabitsAPI'
import { useApiError } from '@/composables/useApiError'
import { fromDateOnlyString, toDateOnlyString } from '@/utils/dateOnly'
import {
  canMarkDay,
  dateRange,
  isHabitScheduled,
  pendingSchedule,
  planStats,
  scheduleOn,
  scheduleWeekdays,
  statusOn,
  weekCompletions,
} from '@/utils/habitTracking'

const props = defineProps<{ habitId: string; initialDate: string; today: string }>()
const emit = defineEmits<{ close: []; edit: [habit: HabitDTO] }>()
const { t, locale } = useI18n()
const store = useHabitsStore()
const areas = useLifeAreasStore()
const goals = useGoalsStore()
const apiError = useApiError()
const visible = ref(true)
const selectedDate = ref(props.initialDate)
const month = ref(props.initialDate.slice(0, 7))
const error = ref('')
const notice = ref('')
const item = computed(() => store.habits.find((h) => h.habit.id === props.habitId))
const busy = computed(() => store.isBusy(props.habitId))
const loading = ref(false)
const monthStart = computed(() => `${month.value}-01`)
const monthEnd = computed(() => {
  const d = fromDateOnlyString(monthStart.value)
  d.setMonth(d.getMonth() + 1, 0)
  return toDateOnlyString(d)
})
const calendar = computed(() => dateRange(monthStart.value, monthEnd.value))
const historyReady = computed(() => {
  const from = store.historyFrom[props.habitId]
  const start = item.value?.habit.startDate ?? monthStart.value
  return !!from && from <= (monthStart.value < start ? start : monthStart.value)
})
const leading = computed(() => (fromDateOnlyString(monthStart.value).getDay() + 6) % 7)
const weekdays = computed(() =>
  Array.from({ length: 7 }, (_, i) => t(`tasks.recurrence.weekdays.${i}`)),
)
const status = computed(() => (item.value ? statusOn(item.value, selectedDate.value) : 'none'))
const scheduled = computed(
  () => item.value && isHabitScheduled(item.value.habit, selectedDate.value),
)
const stats = computed(() =>
  item.value
    ? planStats(item.value, monthStart.value, monthEnd.value, props.today)
    : { done: 0, planned: 0, skipped: 0 },
)
const weeklyGoal = computed(() =>
  item.value ? scheduleOn(item.value.habit, props.today).goal : null,
)
const upcoming = computed(() => item.value && pendingSchedule(item.value.habit, props.today))
const streakLabel = computed(() => {
  const count = Number(item.value?.currentStreak) || 0
  const unit = weeklyGoal.value ? 'week' : 'day'
  const plural = new Intl.PluralRules(locale.value).select(count)
  return `${count} ${t(`habits.streakUnits.${unit}.${plural}`)}`
})
const format = (
  date: string,
  options: Intl.DateTimeFormatOptions = { day: 'numeric', month: 'long' },
) => fromDateOnlyString(date).toLocaleDateString(locale.value, options)
const relations = computed(() =>
  [
    areas.lifeAreas.find((a) => a.id === item.value?.habit.lifeAreaId)?.name,
    goals.getGoalById(item.value?.habit.goalId)?.title,
  ]
    .filter(Boolean)
    .join(' · '),
)
const scheduleLabel = computed(() => {
  if (!item.value) return ''
  const schedule = scheduleOn(item.value.habit, props.today)
  if (schedule.goal) return t('habits.tracker.weekGoal', { goal: schedule.goal })
  const days = scheduleWeekdays(schedule.rule)
  return days.length === 7
    ? t('habits.editdialog.everyDay')
    : days.map((d) => weekdays.value[d]).join(' · ')
})

async function loadMonth() {
  loading.value = true
  error.value = ''
  try {
    await store.ensureHistoryFrom(props.habitId, monthStart.value)
  } catch (e) {
    error.value = apiError.resolveMessage(e)
  } finally {
    loading.value = false
  }
}
watch(
  month,
  () => {
    void loadMonth()
  },
  { immediate: true },
)
watch(visible, (v) => {
  if (!v) emit('close')
})
function moveMonth(offset: number) {
  if (loading.value) return
  const date = fromDateOnlyString(monthStart.value)
  date.setMonth(date.getMonth() + offset)
  month.value = toDateOnlyString(date).slice(0, 7)
  selectedDate.value = month.value === props.today.slice(0, 7) ? props.today : monthStart.value
}
async function setStatus(next: HabitCompletion) {
  if (!item.value || busy.value || loading.value || !historyReady.value) return
  error.value = ''
  notice.value = ''
  try {
    await store.setDayStatus(props.habitId, fromDateOnlyString(selectedDate.value), next)
  } catch (e) {
    error.value = apiError.resolveMessage(e)
  }
}
async function setState(state: HabitState) {
  error.value = ''
  notice.value = ''
  try {
    await store.setState(props.habitId, state)
    notice.value = t('habits.tracker.stateSaved')
  } catch (e) {
    error.value = apiError.resolveMessage(e)
  }
}
</script>

<template>
  <BaseDrawer v-model:visible="visible" class="habit-details-drawer">
    <template #header>
      <div v-if="item" class="habit-detail-title">
        <h2>{{ item.habit.title }}</h2>
        <p>{{ scheduleLabel }}</p>
      </div>
    </template>
    <div v-if="item" class="habit-details">
      <p v-if="relations" class="habit-muted">{{ relations }}</p>
      <Message v-if="error" severity="error" role="alert"
        >{{ error }}<Button :label="t('habits.tracker.retry')" text @click="loadMonth"
      /></Message>
      <p v-if="notice" class="habit-muted" role="status">{{ notice }}</p>
      <p v-if="upcoming" class="habit-muted">
        {{ t('habits.tracker.pendingSchedule', { date: format(upcoming.effectiveFrom) }) }}
      </p>
      <Message v-if="item.habit.state !== 'active'" severity="secondary">
        {{ t('habits.tracker.states.' + item.habit.state) }}.
        {{ t('habits.tracker.historyPreserved') }}
      </Message>
      <div class="habit-detail-stats">
        <div>
          <strong>{{ streakLabel }}</strong
          ><span>{{ t('habits.tracker.streak') }}</span>
        </div>
        <div v-if="weeklyGoal">
          <strong>{{ weekCompletions(item, today) }} / {{ weeklyGoal }}</strong
          ><span>{{ t('habits.tracker.weeklyPlan') }}</span>
        </div>
        <div v-else>
          <strong>{{ historyReady ? `${stats.done} / ${stats.planned}` : '—' }}</strong
          ><span>{{ t('habits.tracker.monthPlan') }}</span>
        </div>
      </div>
      <section>
        <div class="habit-section-heading">
          <h3>{{ t('habits.tracker.history') }}</h3>
          <span class="habit-muted">{{
            t('habits.tracker.skippedCount', { count: stats.skipped })
          }}</span>
        </div>
        <div class="habits-week-navigation">
          <Button
            icon="pi pi-chevron-left"
            text
            rounded
            :aria-label="t('habits.tracker.previousMonth')"
            :disabled="loading || monthStart <= item.habit.startDate"
            @click="moveMonth(-1)"
          />
          <span>{{ format(monthStart, { month: 'long', year: 'numeric' }) }}</span>
          <Button
            icon="pi pi-chevron-right"
            text
            rounded
            :aria-label="t('habits.tracker.nextMonth')"
            :disabled="loading || month >= today.slice(0, 7)"
            @click="moveMonth(1)"
          />
        </div>
        <div v-if="historyReady" class="habit-history-calendar" :aria-busy="loading">
          <span v-for="weekday in weekdays" :key="weekday" class="habit-calendar-weekday">{{
            weekday
          }}</span>
          <span v-for="n in leading" :key="'leading-' + n" />
          <button
            v-for="date in calendar"
            :key="date"
            type="button"
            class="habit-day-button"
            :class="[
              statusOn(item, date),
              {
                'is-selected': date === selectedDate,
                'is-off': !isHabitScheduled(item.habit, date),
              },
            ]"
            :disabled="loading || date > today || date < item.habit.startDate"
            :aria-label="`${format(date)}: ${t('habits.tracker.status.' + statusOn(item, date))}`"
            :aria-pressed="date === selectedDate"
            @click="selectedDate = date"
          >
            <span aria-hidden="true">{{
              statusOn(item, date) === 'full'
                ? '✓'
                : statusOn(item, date) === 'skip'
                  ? '−'
                  : Number(date.slice(-2))
            }}</span>
          </button>
        </div>
        <p class="habit-muted">
          {{ loading ? t('habits.tracker.loadingHistory') : t('habits.tracker.historyLegend') }}
        </p>
      </section>
      <section>
        <div class="habit-section-heading">
          <h3>{{ format(selectedDate) }}</h3>
          <span class="habit-muted" aria-live="polite">{{
            t('habits.tracker.status.' + status)
          }}</span>
        </div>
        <div class="habit-detail-actions">
          <Button
            :label="t('habits.tracker.status.full')"
            icon="pi pi-check"
            :disabled="!scheduled || busy || loading || !historyReady || selectedDate > today"
            @click="setStatus('full')"
          />
          <Button
            :label="t('habits.tracker.skip')"
            severity="secondary"
            outlined
            :disabled="!scheduled || busy || loading || !historyReady || selectedDate > today"
            @click="setStatus('skip')"
          />
          <Button
            :label="t('habits.tracker.clear')"
            severity="secondary"
            text
            :disabled="
              status === 'none' ||
              busy ||
              loading ||
              !historyReady ||
              !canMarkDay(item, selectedDate, today)
            "
            @click="setStatus('none')"
          />
        </div>
        <p v-if="!scheduled" class="habit-muted">{{ t('habits.tracker.notScheduled') }}</p>
      </section>
    </div>
    <template #footer>
      <div v-if="item" class="habit-detail-actions">
        <Button
          :label="t('habits.tracker.configure')"
          icon="pi pi-sliders-h"
          text
          :disabled="busy"
          @click="emit('edit', item.habit)"
        />
        <Button
          v-if="item.habit.state === 'active'"
          :label="t('habits.tracker.pause')"
          icon="pi pi-pause"
          severity="secondary"
          text
          :loading="busy"
          @click="setState('paused')"
        />
        <Button
          v-else
          :label="
            t(item.habit.state === 'archived' ? 'habits.tracker.restore' : 'habits.tracker.resume')
          "
          icon="pi pi-play"
          severity="secondary"
          text
          :loading="busy"
          @click="setState('active')"
        />
        <Button
          v-if="item.habit.state !== 'archived'"
          :label="t('habits.tracker.archive')"
          icon="pi pi-inbox"
          severity="secondary"
          text
          :disabled="busy"
          @click="setState('archived')"
        />
      </div>
    </template>
  </BaseDrawer>
</template>
