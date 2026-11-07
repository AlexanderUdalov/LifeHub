<script setup lang="ts">
import { computed, onMounted, ref, watch, nextTick } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import Button from 'primevue/button'
import TaskEditDialog from '@/components/TaskEditDialog.vue'
import HabitEditDialog from '@/components/HabitEditDialog.vue'
import AddictionEditDialog from '@/components/AddictionEditDialog.vue'
import GoalEditDialog from '@/components/GoalEditDialog.vue'
import LifeAreaEditDialog from '@/components/LifeAreaEditDialog.vue'
import JournalEntryEditDialog from '@/components/JournalEntryEditDialog.vue'
import type { TaskDTO } from '@/api/TasksAPI'
import type { HabitDTO } from '@/api/HabitsAPI'
import type { AddictionDTO } from '@/api/AddictionsAPI'
import type { GoalDTO } from '@/api/GoalsAPI'
import type { LifeAreaDTO } from '@/api/LifeAreasAPI'
import type { JournalEntryDTO } from '@/api/JournalAPI'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { useGoalsStore } from '@/stores/goals'
import { usePreferencesStore } from '@/stores/preferences'
import { navigation } from '@/constants/navigation'

type EditContext =
  | { type: 'task'; item: TaskDTO | null }
  | { type: 'habit'; item: HabitDTO | null }
  | { type: 'addiction'; item: AddictionDTO | null }
  | { type: 'goal'; item: GoalDTO | null }
  | { type: 'lifearea'; item: LifeAreaDTO | null }
  | { type: 'journal'; item: JournalEntryDTO | null }
  | null
const { t } = useI18n()
const route = useRoute()
const preferences = usePreferencesStore()
const lifeAreasStore = useLifeAreasStore()
const goalsStore = useGoalsStore()
const editContext = ref<EditContext>(null)
const taskDate = ref<string | undefined>()
const contentRef = ref<HTMLElement | null>(null)
const scrollByPath: Record<string, number> = {}
const tabViews = [
  'TodayView',
  'TasksView',
  'HabitsView',
  'GoalsView',
  'LifeAreasView',
  'JournalView',
  'AddictionsView',
]
const mobileTabs = computed(() =>
  preferences.mobileTabs.map((key) => navigation.find((x) => x.id === key)!),
)
const current = computed(() =>
  navigation.find((x) => route.path === x.path || route.path.startsWith(x.path + '/')),
)
const moreActive = computed(() => !mobileTabs.value.some((x) => x.id === current.value?.id))
const hideCreate = computed(
  () =>
    ['/more', '/profile', '/appearance', '/navigation'].includes(route.path) ||
    (route.path === '/lifeareas' && lifeAreasStore.isLimitReached),
)
watch(
  () => route.path,
  async (path, old) => {
    if (old) scrollByPath[old] = contentRef.value?.scrollTop ?? 0
    editContext.value = null
    await nextTick()
    if (contentRef.value) contentRef.value.scrollTop = scrollByPath[path] ?? 0
  },
)
onMounted(async () => {
  await Promise.allSettled([lifeAreasStore.fetchLifeAreas(), goalsStore.fetchGoals(true)])
})
function createTask(date?: string) {
  taskDate.value = date
  editContext.value = { type: 'task', item: null }
}
function createPrimary() {
  taskDate.value = undefined
  switch (current.value?.id) {
    case 'journal':
      editContext.value = { type: 'journal', item: null }
      break
    case 'habits':
      editContext.value = { type: 'habit', item: null }
      break
    case 'addictions':
      editContext.value = { type: 'addiction', item: null }
      break
    case 'goals':
      editContext.value = { type: 'goal', item: null }
      break
    case 'lifeareas':
      if (!lifeAreasStore.isLimitReached) editContext.value = { type: 'lifearea', item: null }
      break
    default:
      createTask()
  }
}
</script>
<template>
  <div class="lifehub-layout">
    <aside class="app-sidebar">
      <RouterLink to="/today" class="app-brand"
        ><span class="app-logo"><i aria-hidden="true" class="pi pi-sparkles" /></span
        >lifehub</RouterLink
      >
      <nav aria-label="LifeHub">
        <RouterLink
          v-for="page in navigation"
          :key="page.id"
          :to="page.path"
          :aria-current="current?.id === page.id ? 'page' : undefined"
          ><i aria-hidden="true" :class="page.icon" />{{ t(page.label) }}</RouterLink
        >
      </nav>
    </aside>
    <div class="app-workspace">
      <header class="app-header">
        <RouterLink to="/today" class="app-brand mobile-brand"
          ><span class="app-logo"><i aria-hidden="true" class="pi pi-sparkles" /></span
          >lifehub</RouterLink
        ><span class="app-breadcrumb"
          >{{ t('redesign.mySpace') }} /
          {{ current ? t(current.label) : t('redesign.navigation') }}</span
        >
        <div class="app-header-actions">
          <RouterLink to="/appearance" class="icon-action" :aria-label="t('redesign.appearance')"
            ><i aria-hidden="true" class="pi pi-palette" /></RouterLink
          ><Button
            v-if="!hideCreate"
            class="desktop-create"
            :label="t('redesign.add')"
            icon="pi pi-plus"
            @click="createPrimary"
          />
        </div>
      </header>
      <main ref="contentRef" class="app-content">
        <RouterView v-slot="{ Component }"
          ><KeepAlive :include="tabViews"
            ><component
              :is="Component"
              :key="route.path"
              @edit-task="
                (item: TaskDTO) => {
                  taskDate = undefined
                  editContext = { type: 'task', item }
                }
              "
              @create-task="createTask"
              @edit-habit="(item: HabitDTO) => (editContext = { type: 'habit', item })"
              @edit-addiction="(item: AddictionDTO) => (editContext = { type: 'addiction', item })"
              @edit-goal="(item: GoalDTO) => (editContext = { type: 'goal', item })"
              @edit-lifearea="
                (item: LifeAreaDTO | null) => (editContext = { type: 'lifearea', item })
              "
              @edit-journal="
                (item: JournalEntryDTO | null) => (editContext = { type: 'journal', item })
              " /></KeepAlive
        ></RouterView>
      </main>
      <nav class="mobile-bottom" :aria-label="t('redesign.navigation')">
        <RouterLink
          v-for="tab in mobileTabs.slice(0, 2)"
          :key="tab.id"
          :to="tab.path"
          :aria-current="current?.id === tab.id ? 'page' : undefined"
          ><i aria-hidden="true" :class="tab.icon" /><span>{{
            t(tab.id === 'lifeareas' ? 'redesign.areasShort' : tab.label)
          }}</span></RouterLink
        ><button type="button" @click="createPrimary" :aria-label="t('redesign.add')">
          <span class="mobile-add"><i aria-hidden="true" class="pi pi-plus" /></span
          ><span>{{ t('redesign.add') }}</span></button
        ><RouterLink
          v-if="mobileTabs[2]"
          :to="mobileTabs[2].path"
          :aria-current="current?.id === mobileTabs[2].id ? 'page' : undefined"
          ><i aria-hidden="true" :class="mobileTabs[2].icon" /><span>{{
            t(mobileTabs[2].id === 'lifeareas' ? 'redesign.areasShort' : mobileTabs[2].label)
          }}</span></RouterLink
        ><RouterLink to="/more" :aria-current="moreActive ? 'page' : undefined"
          ><i aria-hidden="true" class="pi pi-ellipsis-h" /><span>{{
            t('redesign.more')
          }}</span></RouterLink
        >
      </nav>
    </div>
    <TaskEditDialog
      v-if="editContext?.type === 'task'"
      :task="editContext.item"
      :initial-date="taskDate"
      @close="editContext = null"
    />
    <HabitEditDialog
      v-if="editContext?.type === 'habit'"
      :habit="editContext.item"
      @close="editContext = null"
    />
    <AddictionEditDialog
      v-if="editContext?.type === 'addiction'"
      :addiction="editContext.item"
      @close="editContext = null"
    />
    <GoalEditDialog
      v-if="editContext?.type === 'goal'"
      :goal="editContext.item"
      @close="editContext = null"
    />
    <LifeAreaEditDialog
      v-if="editContext?.type === 'lifearea'"
      :area="editContext.item"
      @close="editContext = null"
    />
    <JournalEntryEditDialog
      v-if="editContext?.type === 'journal'"
      :entry="editContext.item"
      @close="editContext = null"
    />
  </div>
</template>
