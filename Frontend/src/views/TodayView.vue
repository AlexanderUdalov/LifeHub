<script setup lang="ts">
defineOptions({ name: 'TodayView' })
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import DatePicker from 'primevue/datepicker'
import Message from 'primevue/message'
import TaskCard from '@/components/TaskCard.vue'
import HabitCard from '@/components/HabitCard.vue'
import JournalCard from '@/components/JournalCard.vue'
import DayFeedbackCard from '@/components/DayFeedbackCard.vue'
import { useTasksStore } from '@/stores/tasks'
import { useHabitsStore } from '@/stores/habits'
import { useJournalStore } from '@/stores/journal'
import { useAddictionsStore } from '@/stores/addictions'
import { useNsfwContentStore } from '@/stores/nsfwContent'
import { toDateOnlyString } from '@/utils/dateOnly'
import type { TaskDTO } from '@/api/TasksAPI'
import type { HabitDTO } from '@/api/HabitsAPI'
import type { JournalEntryDTO } from '@/api/JournalAPI'
const emit = defineEmits<{
  (e: 'edit-task', task: TaskDTO): void
  (e: 'edit-habit', habit: HabitDTO): void
  (e: 'edit-journal', entry: JournalEntryDTO | null): void
}>()
const { t } = useI18n()
const tasks = useTasksStore(),
  habits = useHabitsStore(),
  journal = useJournalStore(),
  addictions = useAddictionsStore(),
  nsfw = useNsfwContentStore()
const selected = ref(new Date()),
  error = ref(false)
const dateKey = computed(() => toDateOnlyString(selected.value))
const todayTasks = computed(() =>
  [...tasks.tasks, ...tasks.completedTasksList].filter(
    (x) => x.dueDate?.slice(0, 10) === dateKey.value,
  ),
)
const overdue = computed(() =>
  tasks.tasks.filter((x) => x.dueDate && x.dueDate.slice(0, 10) < dateKey.value),
)
const entries = computed(() =>
  journal.entries
    .filter(
      (x) =>
        toDateOnlyString(new Date(x.createdAt)) === dateKey.value &&
        (!x.addictionId ||
          nsfw.addictionVisible(
            addictions.addictions.find((a) => a.addiction.id === x.addictionId)?.addiction ?? {},
          )),
    )
    .slice(0, 3),
)
async function load() {
  error.value = false
  const result = await Promise.allSettled([
    tasks.fetchTasks(),
    habits.fetchHabits(56),
    journal.loadEntries(),
    addictions.fetchAddictions(60),
  ])
  error.value = result.some((x) => x.status === 'rejected')
}
onMounted(load)
</script>
<template>
  <div class="today-page">
    <div class="page-heading">
      <h1 class="ds-page-header">{{ t('redesign.today') }}</h1>
      <DatePicker
        v-model="selected"
        date-format="dd.mm.yy"
        :manual-input="false"
        show-icon
        :aria-label="t('dayFeedback.date')"
      />
    </div>
    <Message v-if="error" severity="error"
      >{{ t('redesign.loadError') }}
      <button type="button" class="text-action" @click="load">
        {{ t('dayFeedback.retry') }}
      </button></Message
    >
    <div class="today-grid">
      <div>
        <section class="settings-section">
          <div class="section-heading">
            <h2>{{ t('tasks.tasks') }}</h2>
            <RouterLink to="/tasks" class="text-action"
              >{{ t('redesign.all') }} <i aria-hidden="true" class="pi pi-angle-right"
            /></RouterLink>
          </div>
          <p v-if="!todayTasks.length" class="muted">{{ t('redesign.noTasksForDay') }}</p>
          <TaskCard
            v-for="task in todayTasks"
            :key="task.id"
            :task="task"
            no-border
            @edit="emit('edit-task', $event)"
            @completion-change="tasks.toggleTaskCompletion"
          />
        </section>
        <details v-if="overdue.length" class="settings-section">
          <summary>{{ t('tasks.list.overdue') }} · {{ overdue.length }}</summary>
          <TaskCard
            v-for="task in overdue"
            :key="task.id"
            :task="task"
            no-border
            @edit="emit('edit-task', $event)"
            @completion-change="tasks.toggleTaskCompletion"
          />
        </details>
        <section class="settings-section">
          <div class="section-heading">
            <h2>{{ t('habits.habits') }}</h2>
            <RouterLink to="/habits" class="text-action"
              >{{ t('redesign.all') }} <i aria-hidden="true" class="pi pi-angle-right"
            /></RouterLink>
          </div>
          <p v-if="!habits.habitsSorted.length" class="muted">{{ t('habits.empty') }}</p>
          <HabitCard
            v-for="habit in habits.habitsSorted"
            :key="habit.habit.id"
            :habit="habit"
            no-border
            @edit="emit('edit-habit', $event)"
          />
        </section>
      </div>
      <div>
        <DayFeedbackCard />
        <section class="settings-section">
          <div class="section-heading">
            <h2>{{ t('journal.title') }}</h2>
            <button type="button" class="text-action" @click="emit('edit-journal', null)">
              {{ t('redesign.add') }} <i aria-hidden="true" class="pi pi-plus" />
            </button>
          </div>
          <p v-if="!entries.length" class="muted">{{ t('redesign.noEntriesForDay') }}</p>
          <JournalCard
            v-for="entry in entries"
            :key="entry.id"
            :item="entry"
            @edit-journal="emit('edit-journal', $event)"
          />
        </section>
      </div>
    </div>
  </div>
</template>
