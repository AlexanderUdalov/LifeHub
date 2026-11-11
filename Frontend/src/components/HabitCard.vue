<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { HabitDTO, HabitWithHistoryDTO } from '@/api/HabitsAPI'
import { useHabitsStore } from '@/stores/habits'
import { useApiError } from '@/composables/useApiError'
import { getWeekDays, toDateOnlyString } from '@/utils/dateOnly'
import { isHabitScheduled, statusOn } from '@/utils/habitTracking'
import HabitTodayCard from './HabitTodayCard.vue'
import HabitDetailsDrawer from './HabitDetailsDrawer.vue'
import Message from 'primevue/message'
import '@/styles/habits.css'

const props = defineProps<{ habit: HabitWithHistoryDTO; noBorder?: boolean }>()
const emit = defineEmits<{ edit: [habit: HabitDTO] }>()
const store = useHabitsStore()
const { t } = useI18n()
const apiError = useApiError()
const open = ref(false)
const error = ref('')
const today = computed(() => toDateOnlyString(new Date()))
const item = computed(
  () => store.habits.find((h) => h.habit.id === props.habit.habit.id) ?? props.habit,
)
function editHabit(habit: HabitDTO) {
  open.value = false
  emit('edit', habit)
}
async function toggle(day: Date) {
  error.value = ''
  try {
    await store.setDayStatus(
      item.value.habit.id,
      day,
      statusOn(item.value, toDateOnlyString(day)) === 'full' ? 'none' : 'full',
    )
  } catch (e) {
    error.value = apiError.resolveMessage(e)
  }
}
</script>

<template>
  <div class="habit-goal-card" :class="{ 'habit-goal-card--borderless': noBorder }">
    <Message v-if="error" severity="error" role="alert">{{ error }}</Message>
    <HabitTodayCard
      v-if="item.habit.state === 'active' && isHabitScheduled(item.habit, today)"
      :item="item"
      :date="today"
      :today="today"
      :days="getWeekDays(new Date())"
      :busy="store.isBusy(item.habit.id)"
      @open="open = true"
      @toggle="toggle"
    />
    <button
      v-else
      type="button"
      class="habit-name-button habit-name-button--full"
      @click="open = true"
    >
      <span
        ><strong>{{ item.habit.title }}</strong
        ><small>{{
          item.habit.state === 'active'
            ? t('habits.tracker.notScheduled')
            : t('habits.tracker.states.' + item.habit.state)
        }}</small></span
      >
      <i class="pi pi-chevron-right" aria-hidden="true" />
    </button>
    <HabitDetailsDrawer
      v-if="open"
      :habit-id="item.habit.id"
      :initial-date="today"
      :today="today"
      @close="open = false"
      @edit="editHabit"
    />
  </div>
</template>
<style scoped>
.habit-goal-card--borderless :deep(.habit-entry) {
  background: transparent;
  padding-left: 0;
  padding-right: 0;
}
</style>
