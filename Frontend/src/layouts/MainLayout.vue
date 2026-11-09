<script setup lang="ts">
import { useRouter, useRoute } from 'vue-router'
import { computed, onBeforeUnmount, onMounted, ref, watch, nextTick } from 'vue'
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

const tabViewNames: string[] = [
  'TasksView',
  'HabitsView',
  'GoalsView',
  'LifeAreasView',
  'JournalView',
  'AddictionsView',
  'ProfileView',
]

const TAB_PATHS = ['/tasks', '/habits', '/goals', '/lifeareas', '/journal', '/addictions', '/profile']
const slideDirection = ref<'left' | 'right'>('left')

const scrollByPath: Record<string, number> = {}
watch(
  () => route.path,
  (newPath, oldPath) => {
    if (oldPath) scrollByPath[oldPath] = contentRef.value?.scrollTop ?? 0
    const oldIdx = TAB_PATHS.indexOf(oldPath ?? '')
    const newIdx = TAB_PATHS.indexOf(newPath)
    slideDirection.value = newIdx > oldIdx ? 'left' : 'right'
    nextTick(() => {
      if (contentRef.value) contentRef.value.scrollTop = scrollByPath[newPath] ?? 0
    })
  }
)

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
const centerTab = computed(() => ({ label: t('lifeareas.title'), icon: 'pi pi-compass', route: '/lifeareas' }))
const rightTabs = computed(() => [
    { label: t('journal.title'), icon: 'pi pi-book', route: '/journal' },
    { label: t('addictions.addictions'), icon: 'pi pi-ban', route: '/addictions' },
    { label: t('profile'), icon: 'pi pi-user', route: '/profile' },
])

const desktopTabs = computed(() => [
    ...leftTabs.value,
    centerTab.value,
    ...rightTabs.value
])

const DESKTOP_BREAKPOINT = 900
const isDesktop = ref(false)
let desktopMedia: MediaQueryList | null = null

function syncDesktopState() {
    isDesktop.value = desktopMedia?.matches ?? false
}

function setupDesktopMedia() {
    desktopMedia = window.matchMedia(`(min-width: ${DESKTOP_BREAKPOINT}px)`)
    syncDesktopState()
    desktopMedia.addEventListener('change', syncDesktopState)
}

onMounted(async () => {
    setupDesktopMedia()

    await Promise.all([
        lifeAreasStore.fetchLifeAreas(),
        goalsStore.fetchGoals()
    ])
})

onBeforeUnmount(() => {
    desktopMedia?.removeEventListener('change', syncDesktopState)
})

const hideFab = computed(() =>
    route.path.startsWith('/lifeareas') && lifeAreasStore.isLimitReached
)

const canCreatePrimary = computed(() =>
    !hideFab.value &&
    (
        route.path.startsWith('/journal') ||
        route.path.startsWith('/habits') ||
        route.path.startsWith('/addictions') ||
        route.path.startsWith('/lifeareas') ||
        route.path.startsWith('/goals') ||
        route.path.startsWith('/tasks')
    )
)

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
    <div class="main-layout">
        <Transition name="sticky-header">
            <div v-if="showStickyHeader" class="sticky-header" aria-hidden="true">
                <span class="sticky-header-title">{{ currentTitle }}</span>
            </div>
        </Transition>
        <aside v-if="isDesktop" class="desktop-sidebar">
            <div class="desktop-sidebar__brand">
                <img src="/favicon.svg" class="desktop-sidebar__logo" :alt="t('lifeareas.title')" />
                <span class="desktop-sidebar__title">LifeHub</span>
            </div>
            <nav class="desktop-sidebar__nav" aria-label="Main navigation">
                <Button
                    v-for="tab in desktopTabs"
                    :key="tab.route"
                    :label="tab.label"
                    :icon="tab.icon"
                    text
                    class="desktop-nav-item"
                    :class="{ 'desktop-nav-item--active': route.path === tab.route }"
                    @click="router.push(tab.route)"
                />
            </nav>
        </aside>
        <aside v-if="isDesktop" class="desktop-balance-rail" aria-hidden="true" />

        <main ref="contentRef" class="content" :class="{ 'content--desktop': isDesktop }" @scroll="onContentScroll">
            <div class="content-frame">
                <RouterView v-slot="{ Component }">
                    <KeepAlive :include="tabViewNames">
                        <Transition :name="'tab-slide-' + slideDirection" mode="out-in">
                            <component :is="Component" :key="route.path"
                                @edit-task="(task: TaskDTO | null) => editContext = { type: 'task', item: task }"
                                @edit-habit="(habit: HabitDTO | null) => editContext = { type: 'habit', item: habit }"
                                @edit-addiction="(addiction: AddictionDTO | null) => editContext = { type: 'addiction', item: addiction }"
                                @edit-lifearea="(area: LifeAreaDTO | null) => editContext = { type: 'lifearea', item: area }"
                                @edit-goal="(goal: GoalDTO | null) => editContext = { type: 'goal', item: goal }"
                                @edit-journal="(entry: JournalEntryDTO | null) => editContext = { type: 'journal', item: entry }" />
                        </Transition>
                    </KeepAlive>
                </RouterView>
            </div>
        </main>

        <Button v-if="!isDesktop && canCreatePrimary" class="fab" icon="pi pi-plus" size="large" rounded @click="createPrimary" />

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

        <Tabs v-if="!isDesktop" class="mobile-tabs" :value="route.path" @update:value="(v) => router.push(v as string)">
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

.tab-slide-left-enter-active,
.tab-slide-left-leave-active {
    transition: transform 0.1s ease;
}

.tab-slide-left-enter-from {
    transform: translateX(100%);
}

.tab-slide-left-leave-to {
    transform: translateX(-100%);
}

.tab-slide-right-enter-active,
.tab-slide-right-leave-active {
    transition: transform 0.1s ease;
}

.tab-slide-right-enter-from {
    transform: translateX(-100%);
}

.tab-slide-right-leave-to {
    transform: translateX(100%);
}

.content {
    flex: 1;
    overflow-x: hidden;
    overflow-y: auto;
    max-height: calc(100dvh - 80px);
}

.content-frame {
    width: 100%;
}

.desktop-sidebar {
    display: none;
}

.desktop-balance-rail {
    display: none;
}

.mobile-tabs {
    display: block;
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
    height: 80px;
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
    width: 3.5rem;
    height: 3.5rem;
    font-size: 1.25rem;
}

@media (min-width: 900px) {
    .main-layout {
        min-height: 100dvh;
    }

    .sticky-header {
        display: none;
    }

    .tab-slide-left-enter-active,
    .tab-slide-left-leave-active,
    .tab-slide-right-enter-active,
    .tab-slide-right-leave-active {
        transition: opacity 0.12s ease;
    }

    .tab-slide-left-enter-from,
    .tab-slide-left-leave-to,
    .tab-slide-right-enter-from,
    .tab-slide-right-leave-to {
        opacity: 0;
        transform: none;
    }

    .content--desktop {
        max-height: 100dvh;
        min-height: 100dvh;
        margin-left: 17rem;
        padding: 2rem clamp(1.5rem, 4vw, 4rem);
        box-sizing: border-box;
    }

    .content-frame {
        max-width: 60rem;
        margin: 0 auto;
    }

    .desktop-sidebar {
        display: flex;
        position: fixed;
        top: 0;
        left: 0;
        width: 17rem;
        height: 100dvh;
        border-right: 1px solid var(--p-content-border-color);
        background: var(--p-content-background);
        box-shadow: 0.5rem 0 2rem rgba(0, 0, 0, 0.04);
        flex-direction: column;
        padding: 1.25rem 0.875rem;
        box-sizing: border-box;
    }

    .desktop-sidebar__brand {
        display: flex;
        align-items: center;
        gap: 0.625rem;
        margin-bottom: 1rem;
        padding: 0 0.75rem;
    }

    .desktop-sidebar__logo {
        width: 2rem;
        height: 2rem;
    }

    .desktop-sidebar__title {
        font-size: 1.2rem;
        font-weight: 700;
        color: var(--p-text-color);
    }

    .desktop-sidebar__nav {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
    }

    .desktop-nav-item.p-button {
        display: flex;
        align-items: center;
        justify-content: flex-start;
        gap: 0.75rem;
        width: 100%;
        min-height: 3rem;
        padding: 0 0.875rem;
        border-radius: var(--ds-radius-md);
        color: var(--p-text-muted-color);
        font-size: 0.95rem;
        text-align: left;
        transition: background-color 0.15s ease, color 0.15s ease;
    }

    .desktop-nav-item.p-button:hover {
        background: var(--p-content-hover-background);
        color: var(--p-text-color);
    }

    .desktop-nav-item--active.p-button {
        background: color-mix(in srgb, var(--p-primary-color) 16%, transparent);
        color: var(--p-text-color);
        font-weight: 600;
    }

    .desktop-nav-item :deep(.p-button-label) {
        flex: 0 1 auto;
        text-align: left;
    }

    .mobile-tabs {
        display: none !important;
    }

    .fab {
        right: 2rem;
        bottom: 2rem;
        width: 3.25rem;
        height: 3.25rem;
        box-shadow: 0 1rem 2rem rgba(0, 0, 0, 0.18);
    }
}

@media (min-width: 1200px) {
    .content--desktop {
        margin-right: 17rem;
    }

    .desktop-balance-rail {
        display: block;
        position: fixed;
        top: 0;
        right: 0;
        width: 17rem;
        height: 100dvh;
        pointer-events: none;
    }
}
</style>
