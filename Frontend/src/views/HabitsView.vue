<script setup lang="ts">
defineOptions({ name: 'HabitsView' })
import { computed, onActivated, onBeforeUnmount, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import Button from 'primevue/button'
import SelectButton from 'primevue/selectbutton'
import Skeleton from 'primevue/skeleton'
import Message from 'primevue/message'
import EmptyState from '@/components/EmptyState.vue'
import HabitTodayCard from '@/components/HabitTodayCard.vue'
import HabitWeekStrip from '@/components/HabitWeekStrip.vue'
import HabitDetailsDrawer from '@/components/HabitDetailsDrawer.vue'
import { useHabitsStore } from '@/stores/habits'
import type { HabitDTO, HabitState, HabitWithHistoryDTO } from '@/api/HabitsAPI'
import { useApiError } from '@/composables/useApiError'
import { fromDateOnlyString, getWeekDays, toDateOnlyString } from '@/utils/dateOnly'
import {
  canMarkDay,
  isHabitScheduled,
  scheduleOn,
  statusOn,
  weekCompletions,
} from '@/utils/habitTracking'
import '@/styles/habits.css'

const emit = defineEmits<{ 'edit-habit': [habit: HabitDTO | null] }>()
const { t, locale } = useI18n()
const store = useHabitsStore()
const apiError = useApiError()
const today = ref(toDateOnlyString(new Date()))
const date = ref(today.value)
const tab = ref<'today' | 'week' | 'all'>('today')
const filter = ref<HabitState>('active')
const detailId = ref<string | null>(null)
const error = ref('')
const historyLoading = ref(false)
const active = computed(() => store.habitsSorted.filter((h) => h.habit.state === 'active'))
const days = computed(() => getWeekDays(fromDateOnlyString(date.value)))
const tabs = computed(() => [
  { label: t('habits.tracker.today'), value: 'today' },
  { label: t('habits.tracker.week'), value: 'week' },
  { label: t('habits.tracker.all'), value: 'all' },
])
const filters: HabitState[] = ['active', 'paused', 'archived']
const due = computed(() =>
  active.value.filter(
    (h) => !scheduleOn(h.habit, date.value).goal && isHabitScheduled(h.habit, date.value),
  ),
)
const flexible = computed(() =>
  active.value.filter(
    (h) => scheduleOn(h.habit, date.value).goal && isHabitScheduled(h.habit, date.value),
  ),
)
const done = computed(() => due.value.filter((h) => statusOn(h, date.value) === 'full').length)
const skipped = computed(() => due.value.filter((h) => statusOn(h, date.value) === 'skip').length)
const planned = computed(() => due.value.length - skipped.value)
const filtered = computed(() => store.habitsSorted.filter((h) => h.habit.state === filter.value))
const currentWeek = computed(() => days.value.some((d) => toDateOnlyString(d) === today.value))
const weekReady = computed(() =>
  active.value.every((h) => {
    const from = toDateOnlyString(days.value[0]!)
    const needed = from < h.habit.startDate ? h.habit.startDate : from
    return !!store.historyFrom[h.habit.id] && store.historyFrom[h.habit.id]! <= needed
  }),
)
const format = (
  day: string | Date,
  options: Intl.DateTimeFormatOptions = { day: 'numeric', month: 'long' },
) =>
  (typeof day === 'string' ? fromDateOnlyString(day) : day).toLocaleDateString(
    locale.value,
    options,
  )
function scheduleLabel(h: HabitDTO, day = today.value) {
  const goal = scheduleOn(h, day).goal
  if (goal) return t('habits.tracker.weekGoal', { goal })
  return t('habits.tracker.fixedSchedule')
}

async function load() {
  error.value = ''
  try {
    await store.fetchHabits()
  } catch (e) {
    error.value = apiError.resolveMessage(e)
  }
}
function updateToday() {
  const next = toDateOnlyString(new Date())
  if (next !== today.value) {
    if (date.value === today.value) date.value = next
    today.value = next
    void load()
  }
}
let tick: ReturnType<typeof setInterval> | undefined
onMounted(() => {
  void load()
  tick = setInterval(updateToday, 30000)
})
onActivated(updateToday)
onBeforeUnmount(() => {
  if (tick) clearInterval(tick)
})

async function moveWeek(offset: number) {
  if (historyLoading.value) return
  const next = fromDateOnlyString(date.value)
  next.setDate(next.getDate() + offset * 7)
  date.value = toDateOnlyString(next) > today.value ? today.value : toDateOnlyString(next)
  await loadWeekHistory()
}
async function loadWeekHistory() {
  historyLoading.value = true
  error.value = ''
  try {
    const from = toDateOnlyString(days.value[0]!)
    await Promise.all(active.value.map((h) => store.ensureHistoryFrom(h.habit.id, from)))
  } catch (e) {
    error.value = apiError.resolveMessage(e)
  } finally {
    historyLoading.value = false
  }
}
async function retry() {
  if (tab.value !== 'all' && !weekReady.value) await loadWeekHistory()
  else await load()
}
function editHabit(habit: HabitDTO) {
  detailId.value = null
  emit('edit-habit', habit)
}
async function toggle(item: HabitWithHistoryDTO, day: Date) {
  const key = toDateOnlyString(day)
  if (!canMarkDay(item, key, today.value) || historyLoading.value || !weekReady.value) return
  error.value = ''
  try {
    await store.setDayStatus(item.habit.id, day, statusOn(item, key) === 'full' ? 'none' : 'full')
  } catch (e) {
    error.value = apiError.resolveMessage(e)
  }
}
</script>

<template>
  <div class="habits-view">
    <header class="habits-header">
      <div class="habits-heading">
        <h1 class="ds-page-header">{{ t('habits.habits') }}</h1>
        <Button
          :label="t('habits.editdialog.newHabit')"
          icon="pi pi-plus"
          class="habits-desktop-create"
          @click="emit('edit-habit', null)"
        />
        <Button
          icon="pi pi-sliders-h"
          text
          rounded
          :aria-label="t('habits.tracker.manage')"
          class="habits-manage"
          @click="tab = 'all'"
        />
      </div>
      <p class="habits-date">
        {{ format(today, { weekday: 'long', day: 'numeric', month: 'long' }) }}
      </p>
      <SelectButton
        v-model="tab"
        :options="tabs"
        option-label="label"
        option-value="value"
        :allow-empty="false"
        class="habits-view-switch"
        :aria-label="t('habits.tracker.view')"
      />
    </header>

    <div class="habits-body">
      <Message v-if="error" severity="error" class="habits-error" role="alert">
        {{ error }} <Button :label="t('habits.tracker.retry')" text @click="retry" />
      </Message>
      <div v-if="store.isLoading && !store.habits.length" class="habit-items">
        <Skeleton v-for="i in 3" :key="i" height="8rem" />
      </div>
      <EmptyState
        v-else-if="!store.habits.length"
        icon="pi pi-sync"
        :title="t('habits.empty')"
        :subtitle="t('habits.emptySubtitle')"
      />
      <template v-else>
        <template v-if="tab !== 'all'">
          <div class="habits-week-navigation">
            <Button
              icon="pi pi-chevron-left"
              text
              rounded
              :aria-label="t('habits.tracker.previousWeek')"
              :disabled="historyLoading"
              @click="moveWeek(-1)"
            />
            <span>{{ format(days[0]!) }} — {{ format(days[6]!) }}</span>
            <Button
              icon="pi pi-chevron-right"
              text
              rounded
              :aria-label="t('habits.tracker.nextWeek')"
              :disabled="currentWeek || historyLoading"
              @click="moveWeek(1)"
            />
          </div>
          <Button
            v-if="!currentWeek || date !== today"
            :label="t('habits.tracker.backToday')"
            text
            size="small"
            @click="date = today"
          />
          <div class="habit-week-heading">
            <button
              v-for="day in days"
              :key="toDateOnlyString(day)"
              type="button"
              class="habit-date-button"
              :class="{ selected: toDateOnlyString(day) === date }"
              :disabled="toDateOnlyString(day) > today || historyLoading"
              :aria-pressed="toDateOnlyString(day) === date"
              :aria-label="format(day)"
              @click="date = toDateOnlyString(day)"
            >
              <span>{{ format(day, { weekday: 'short' }) }}</span
              ><strong>{{ day.getDate() }}</strong>
            </button>
          </div>
          <p v-if="historyLoading" class="habit-muted" role="status">
            {{ t('habits.tracker.loadingHistory') }}
          </p>
        </template>

        <div v-if="tab !== 'all' && (historyLoading || !weekReady)" class="habit-items">
          <Skeleton v-for="i in 3" :key="i" height="8rem" />
        </div>
        <template v-else-if="tab === 'today'">
          <div class="habit-section-heading">
            <h2>
              {{
                date === today
                  ? t('habits.tracker.dailyPlan')
                  : t('habits.tracker.planOn', { date: format(date) })
              }}
            </h2>
            <span class="habit-muted" aria-live="polite"
              >{{ t('habits.tracker.planProgress', { done, total: planned })
              }}<template v-if="skipped">
                · {{ t('habits.tracker.skippedCount', { count: skipped }) }}</template
              ></span
            >
          </div>
          <div
            class="habit-plan-progress"
            role="progressbar"
            :aria-label="t('habits.tracker.dailyPlan')"
            :aria-valuenow="done"
            :aria-valuemin="0"
            :aria-valuemax="Math.max(1, planned)"
          >
            <span :style="{ width: `${planned ? (done / planned) * 100 : 0}%` }" />
          </div>
          <div class="habit-items">
            <HabitTodayCard
              v-for="item in due"
              :key="item.habit.id"
              :item="item"
              :date="date"
              :today="today"
              :days="days"
              :busy="store.isBusy(item.habit.id) || historyLoading"
              @open="detailId = item.habit.id"
              @toggle="toggle(item, $event)"
            />
            <p v-if="!due.length" class="habit-empty">{{ t('habits.tracker.noPlan') }}</p>
          </div>
          <template v-if="flexible.length">
            <div class="habit-section-heading">
              <h2>{{ t('habits.tracker.flexible') }}</h2>
              <span class="habit-muted">{{ t('habits.tracker.weeklyPlan') }}</span>
            </div>
            <div class="habit-items">
              <HabitTodayCard
                v-for="item in flexible"
                :key="item.habit.id"
                :item="item"
                :date="date"
                :today="today"
                :days="days"
                :busy="store.isBusy(item.habit.id) || historyLoading"
                @open="detailId = item.habit.id"
                @toggle="toggle(item, $event)"
              />
            </div>
          </template>
        </template>

        <div v-else-if="tab === 'week'" class="habit-items">
          <article v-for="item in active" :key="item.habit.id" class="habit-entry">
            <div class="habit-entry-top">
              <button type="button" class="habit-name-button" @click="detailId = item.habit.id">
                <strong>{{ item.habit.title }}</strong
                ><small>{{ scheduleLabel(item.habit, date) }}</small>
              </button>
              <span v-if="scheduleOn(item.habit, date).goal" class="habit-week-progress">
                {{ weekCompletions(item, date) }} / {{ scheduleOn(item.habit, date).goal }}
              </span>
            </div>
            <HabitWeekStrip
              :item="item"
              :days="days"
              :today="today"
              :selected-date="date"
              :busy="store.isBusy(item.habit.id) || historyLoading"
              @toggle="toggle(item, $event)"
            />
          </article>
          <p class="habit-muted">{{ t('habits.tracker.weekLegend') }}</p>
        </div>

        <template v-else>
          <div class="habit-filters" :aria-label="t('habits.tracker.stateFilter')">
            <button
              v-for="state in filters"
              :key="state"
              type="button"
              :aria-pressed="filter === state"
              @click="filter = state"
            >
              {{ t('habits.tracker.states.' + state) }}
              {{ store.habits.filter((h) => h.habit.state === state).length }}
            </button>
          </div>
          <div class="habit-items">
            <article v-for="item in filtered" :key="item.habit.id" class="habit-entry">
              <button
                type="button"
                class="habit-name-button habit-name-button--full"
                @click="detailId = item.habit.id"
              >
                <span
                  ><strong>{{ item.habit.title }}</strong
                  ><small>{{ scheduleLabel(item.habit) }}</small></span
                >
                <i class="pi pi-chevron-right" aria-hidden="true" />
              </button>
            </article>
            <p v-if="!filtered.length" class="habit-empty">
              {{ t('habits.tracker.emptyState.' + filter) }}
            </p>
          </div>
        </template>
      </template>
    </div>
    <HabitDetailsDrawer
      v-if="detailId"
      :habit-id="detailId"
      :initial-date="date"
      :today="today"
      @close="detailId = null"
      @edit="editHabit"
    />
  </div>
</template>
