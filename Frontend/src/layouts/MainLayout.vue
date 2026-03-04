<script setup lang="ts">
import { useRouter, useRoute } from 'vue-router'
import { computed, onMounted, ref } from 'vue'
import Tabs from 'primevue/tabs'
import Tab from 'primevue/tab'
import TabList from 'primevue/tablist'
import Button from 'primevue/button'

import TaskEditDialog from '@/components/TaskEditDialog.vue'
import HabitEditDialog from '@/components/HabitEditDialog.vue'
import AddictionEditDialog from '@/components/AddictionEditDialog.vue'
import GoalEditDialog from '@/components/GoalEditDialog.vue'
import LifeAreaEditDialog from '@/components/LifeAreaEditDialog.vue'
import JournalEntryEditDialog from '@/components/JournalEntryEditDialog.vue'
import type { GoalDTO } from '@/api/GoalsAPI'
import type { JournalEntryDTO } from '@/api/JournalAPI'
import type { HabitDTO } from '@/api/HabitsAPI'
import type { AddictionDTO } from '@/api/AddictionsAPI'
import type { LifeAreaDTO } from '@/api/LifeAreasAPI'
import { type TaskDTO } from '@/api/TasksAPI'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { useGoalsStore } from '@/stores/goals'

import { useI18n } from 'vue-i18n'
const { t } = useI18n()

type EditContext =
    | { type: 'task'; item: TaskDTO | null }
    | { type: 'habit'; item: HabitDTO | null }
    | { type: 'addiction'; item: AddictionDTO | null }
    | { type: 'lifearea'; item: LifeAreaDTO | null }
    | { type: 'goal'; item: GoalDTO | null }
    | { type: 'journal'; item: JournalEntryDTO | null }
    | null

const editContext = ref<EditContext>(null)
const lifeAreasStore = useLifeAreasStore()
const goalsStore = useGoalsStore()

const router = useRouter()
const route = useRoute()

const contentRef = ref<HTMLElement | null>(null)
const contentScrollTop = ref(0)
const SCROLL_THRESHOLD = 8

const currentTitle = computed(() => {
    const key = (route.meta as { titleKey?: string }).titleKey
    return key ? t(key) : ''
})

const showStickyHeader = computed(() => currentTitle.value && contentScrollTop.value > SCROLL_THRESHOLD)

function onContentScroll() {
    contentScrollTop.value = contentRef.value?.scrollTop ?? 0
}

const leftTabs = computed(() => [
    { label: t('tasks.tasks'), icon: 'pi pi-check-square', route: '/tasks' },
    { label: t('habits.habits'), icon: 'pi pi-calendar', route: '/habits' },
    { label: t('goals.title'), icon: 'pi pi-bullseye', route: '/goals' },
])
const centerTab = { route: '/lifeareas' }
const rightTabs = computed(() => [
    { label: t('journal.title'), icon: 'pi pi-book', route: '/journal' },
    { label: t('addictions.addictions'), icon: 'pi pi-ban', route: '/addictions' },
    { label: t('profile'), icon: 'pi pi-user', route: '/profile' },
])

onMounted(async () => {
    await Promise.all([
        lifeAreasStore.fetchLifeAreas(),
        goalsStore.fetchGoals()
    ])
})

const createPrimary = () => {
    if (route.path.startsWith('/journal')) {
        editContext.value = { type: 'journal', item: null }
        return
    }
    if (route.path.startsWith('/habits')) {
        editContext.value = { type: 'habit', item: null }
        return
    }
    if (route.path.startsWith('/addictions')) {
        editContext.value = { type: 'addiction', item: null }
        return
    }
    if (route.path.startsWith('/lifeareas')) {
        editContext.value = { type: 'lifearea', item: null }
        return
    }
    if (route.path.startsWith('/goals')) {
        editContext.value = { type: 'goal', item: null }
        return
    }
    editContext.value = { type: 'task', item: null }
}

</script>

<template>
    <div>
        <Transition name="sticky-header">
            <div v-if="showStickyHeader" class="sticky-header" aria-hidden="true">
                <span class="sticky-header-title">{{ currentTitle }}</span>
            </div>
        </Transition>
        <main ref="contentRef" class="content" @scroll="onContentScroll">
            <RouterView v-slot="{ Component }">
                <component :is="Component" @edit-task="(task: TaskDTO) => editContext = { type: 'task', item: task }"
                    @edit-habit="(habit: HabitDTO) => editContext = { type: 'habit', item: habit }"
                    @edit-addiction="(addiction: AddictionDTO) => editContext = { type: 'addiction', item: addiction }"
                    @edit-lifearea="(area: LifeAreaDTO) => editContext = { type: 'lifearea', item: area }"
                    @edit-goal="(goal: GoalDTO) => editContext = { type: 'goal', item: goal }"
                    @edit-journal="(entry: JournalEntryDTO | null) => editContext = { type: 'journal', item: entry }" />
            </RouterView>
        </main>

        <Button class="fab" icon="pi pi-plus" size="large" rounded @click="createPrimary" />

        <TaskEditDialog v-if="editContext && editContext.type === 'task'" :task="editContext.item"
            @close="editContext = null" />
        <HabitEditDialog v-if="editContext && editContext.type === 'habit'" :habit="editContext.item"
            @close="editContext = null" />
        <AddictionEditDialog v-if="editContext && editContext.type === 'addiction'" :addiction="editContext.item"
            @close="editContext = null" />
        <LifeAreaEditDialog v-if="editContext && editContext.type === 'lifearea'" :area="editContext.item"
            @close="editContext = null" />
        <GoalEditDialog v-if="editContext && editContext.type === 'goal'" :goal="editContext.item"
            @close="editContext = null" />
        <JournalEntryEditDialog v-if="editContext && editContext.type === 'journal'" :entry="editContext.item"
            @close="editContext = null" />

        <Tabs :value="route.path" @update:value="(v) => router.push(v as string)">
            <TabList class="tab-list">
                <div class="tab-group tab-group-left">
                    <Tab v-for="tab in leftTabs" :key="tab.route" :value="tab.route" class="tab">
                        <i :class="tab.icon" />
                        <span class="tab-label">{{ tab.label }}</span>
                    </Tab>
                </div>
                <Tab :value="centerTab.route" class="tab tab-center">
                    <img src="/favicon.svg" class="tab-logo" :alt="t('lifeareas.title')" />
                </Tab>
                <div class="tab-group tab-group-right">
                    <Tab v-for="tab in rightTabs" :key="tab.route" :value="tab.route" class="tab">
                        <i :class="tab.icon" />
                        <span class="tab-label">{{ tab.label }}</span>
                    </Tab>
                </div>
            </TabList>
        </Tabs>
    </div>
</template>

<style scoped>
.sticky-header {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    z-index: 10;
    height: 52px;
    display: flex;
    align-items: center;
    justify-content: center;
    pointer-events: none;
    background: linear-gradient(to bottom,
            #000000ae 0%,
            transparent 100%);
}

.sticky-header-title {
    font-size: 1rem;
    font-weight: 600;
    color: var(--p-text-color);
    text-align: center;
}

.sticky-header-enter-active,
.sticky-header-leave-active {
    transition: opacity 0.2s ease;
}

.sticky-header-enter-from,
.sticky-header-leave-to {
    opacity: 0;
}

.content {
    flex: 1;
    overflow-y: auto;
    max-height: calc(100dvh - 62px);
}

.tab-list {
    display: flex;
    width: 100%;
    overflow: hidden;
    align-items: stretch;
}

.tab-group {
    flex: 1;
    min-width: 0;
    display: flex;
    overflow: hidden;
}

.tab-group-left {
    justify-content: flex-end;
}

.tab-group-right {
    justify-content: flex-start;
}

.tab {
    flex: 1;
    min-width: 0;
    min-height: 4px;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 0.25rem;
    overflow: hidden;
}

.tab-center {
    flex: 0 0 auto;
    width: 3.5rem;
}

.tab-label {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    max-width: 100%;
    font-size: 0.7rem;
}

.tab-logo {
    width: 1.8rem;
    height: 1.8rem;
    display: block;
}

:deep(.p-tabs) {
    position: fixed;
    bottom: 0;
    left: 0;
    right: 0;
    width: 100%;
    overflow: hidden;
    border-top: 1px;
    border-top-style: solid;
    border-color: var(--p-content-border-color);
}

:deep(.p-tablist) {
    overflow: hidden;
}

@media (max-width: 420px) {
    .tab-label {
        display: none;
    }
}

.fab {
    position: fixed;
    right: 1rem;
    bottom: 7rem;
}
</style>
