<script setup lang="ts">
defineOptions({ name: 'JournalView' })
import EmptyState from '@/components/EmptyState.vue';
import JournalCard from '@/components/JournalCard.vue';
import ReflectionDialog from '@/components/ReflectionDialog.vue';
import Skeleton from 'primevue/skeleton';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Dropdown from 'primevue/dropdown';
import { useJournalStore } from '@/stores/journal';
import { useLifeAreasStore } from '@/stores/lifeAreas';
import { useGoalsStore } from '@/stores/goals';
import { useAddictionsStore } from '@/stores/addictions';
import { useNsfwContentStore } from '@/stores/nsfwContent';
import { useI18n } from 'vue-i18n';
import { computed, onMounted, ref, watch } from 'vue';
import type { JournalEntryDTO } from '@/api/JournalAPI';

const journalStore = useJournalStore();
const lifeAreasStore = useLifeAreasStore();
const goalsStore = useGoalsStore();
const addictionsStore = useAddictionsStore();
const nsfwContentStore = useNsfwContentStore();
const { t } = useI18n();

const searchQuery = ref('');
const showSearchBar = ref(false);
const showFilterBar = ref(false);
const filterLifeAreaId = ref<string | null>(null);
const filterGoalId = ref<string | null>(null);
const filterAddictionId = ref<string | null>(null);

const lifeAreaOptions = computed(() => [
  { label: t('journal.filterAll'), value: null },
  ...lifeAreasStore.lifeAreas.map((a) => ({ label: a.name, value: a.id }))
]);

const goalOptions = computed(() => [
  { label: t('journal.filterAll'), value: null },
  ...goalsStore.goalsSorted.map((g) => ({ label: g.title, value: g.id }))
]);

const addictionOptions = computed(() => [
  { label: t('journal.filterAll'), value: null },
  ...addictionsStore.addictions
    .filter((a) => nsfwContentStore.addictionVisible(a.addiction))
    .map((a) => ({ label: a.addiction.title, value: a.addiction.id }))
]);

const addictionsById = computed(
  () => new Map(addictionsStore.addictions.map((row) => [row.addiction.id, row.addiction]))
);

function journalEntryVisible(entry: JournalEntryDTO) {
  if (!entry.addictionId) return true;
  const addiction = addictionsById.value.get(entry.addictionId);
  if (!addiction) return true;
  return nsfwContentStore.addictionVisible(addiction);
}

watch(
  () =>
    `${nsfwContentStore.showNsfwContent}|${addictionsStore.addictions
      .map((a) => `${a.addiction.id}:${a.addiction.isNsfw ? 1 : 0}`)
      .join(',')}`,
  () => {
    const id = filterAddictionId.value;
    if (!id) return;
    const row = addictionsStore.addictions.find((a) => a.addiction.id === id);
    if (row && !nsfwContentStore.addictionVisible(row.addiction)) filterAddictionId.value = null;
  }
);

const filteredEntries = computed(() => {
  let list = journalStore.entries.filter(journalEntryVisible);

  const q = searchQuery.value.trim().toLowerCase();
  if (q) {
    list = list.filter((e) => e.text.toLowerCase().includes(q));
  }
  if (filterLifeAreaId.value) {
    list = list.filter((e) => e.lifeAreaId === filterLifeAreaId.value);
  }
  if (filterGoalId.value) {
    list = list.filter((e) => e.goalId === filterGoalId.value);
  }
  if (filterAddictionId.value) {
    list = list.filter((e) => e.addictionId === filterAddictionId.value);
  }

  const pinned = list.filter((e) => e.isPinned);
  const regular = list.filter((e) => !e.isPinned);
  return { pinned, regular };
});

const pinnedItems = computed(() => filteredEntries.value.pinned);
const regularItems = computed(() => filteredEntries.value.regular);
const hasEntries = computed(() => journalStore.entries.length > 0);
const hasFilteredResults = computed(
  () => pinnedItems.value.length > 0 || regularItems.value.length > 0
);
const isFilterActive = computed(
  () => !!filterLifeAreaId.value || !!filterGoalId.value || !!filterAddictionId.value
);

function toggleSearchBar() {
  showSearchBar.value = !showSearchBar.value;
}

function toggleFilterBar() {
  showFilterBar.value = !showFilterBar.value;
}

function clearFilters() {
  filterLifeAreaId.value = null;
  filterGoalId.value = null;
  filterAddictionId.value = null;
}

const showReflection = ref(false);

const emit = defineEmits<{
  (e: 'edit-journal', entry: JournalEntryDTO | null): void
}>();

onMounted(async () => {
  await Promise.all([
    journalStore.loadEntries(),
    addictionsStore.fetchAddictions(60).catch(() => undefined)
  ]);
});
</script>

<template>
  <div class="journal-view">
    <header class="journal-view__header">
      <h1 class="ds-page-header">{{ $t('journal.title') }}</h1>
      <div class="journal-view__actions">
        <Button icon="pi pi-sparkles" variant="text" rounded size="small"
          class="journal-view__action-btn"
          :aria-label="$t('reflection.title')" @click="showReflection = true" />
        <Button icon="pi pi-search" variant="text" rounded size="small"
          :severity="showSearchBar ? 'secondary' : undefined" class="journal-view__action-btn"
          :aria-label="$t('journal.search')" @click="toggleSearchBar" />
        <Button icon="pi pi-filter" variant="text" rounded size="small"
          :severity="isFilterActive ? 'secondary' : undefined" class="journal-view__action-btn"
          :aria-label="$t('journal.filter')" @click="toggleFilterBar" />
      </div>
    </header>

    <Transition name="journal-search-slide">
      <div v-if="showSearchBar" class="journal-view__search-bar">
        <i class="pi pi-search journal-view__search-icon" />
        <InputText v-model="searchQuery" type="text" :placeholder="$t('journal.searchPlaceholder')"
          class="journal-view__search-input" />
      </div>
    </Transition>

    <Transition name="journal-search-slide">
      <div v-if="showFilterBar" class="journal-view__filter-bar">
        <label class="journal-view__filter-label">{{ $t('journal.filterByLifeArea') }}</label>
        <Dropdown v-model="filterLifeAreaId" :options="lifeAreaOptions" option-label="label" option-value="value"
          :placeholder="$t('lifeareas.selectPlaceholder')" class="journal-view__filter-dropdown" />
        <label class="journal-view__filter-label">{{ $t('journal.filterByGoal') }}</label>
        <Dropdown v-model="filterGoalId" :options="goalOptions" option-label="label" option-value="value"
          :placeholder="$t('goals.selectPlaceholder')" class="journal-view__filter-dropdown" />
        <label class="journal-view__filter-label">{{ $t('journal.filterByAddiction') }}</label>
        <Dropdown v-model="filterAddictionId" :options="addictionOptions" option-label="label" option-value="value"
          :placeholder="$t('addictions.addictions')" class="journal-view__filter-dropdown" />
        <Button :label="$t('journal.clearFilters')" variant="text" size="small" class="journal-view__filter-clear"
          @click="clearFilters" />
      </div>
    </Transition>

    <div v-if="journalStore.isLoading && journalStore.entries.length === 0" class="journal-skeleton">
      <div v-for="i in 3" :key="i" class="skeleton-card">
        <Skeleton width="100%" height="4.5rem" borderRadius="16px" />
      </div>
    </div>

    <EmptyState v-else-if="!hasEntries" icon="pi pi-book" :title="$t('journal.empty')"
      :subtitle="$t('journal.emptySubtitle')" />

    <EmptyState v-else-if="!hasFilteredResults" icon="pi pi-search" :title="$t('journal.noResults')"
      :subtitle="$t('journal.noResultsHint')" />

    <template v-else>
      <section v-if="pinnedItems.length" class="journal-section">
        <div class="journal-section__header">
          <i class="pi pi-thumbtack journal-section__icon" />
          <span>{{ $t('journal.pinned') }}</span>
        </div>
        <TransitionGroup name="journal-list" tag="div" class="journal-cards">
          <JournalCard v-for="item in pinnedItems" :key="item.id" :item="item"
            @edit-journal="(entry) => emit('edit-journal', entry)" />
        </TransitionGroup>
      </section>

      <section class="journal-section">
        <TransitionGroup name="journal-list" tag="div" class="journal-cards">
          <JournalCard v-for="item in regularItems" :key="item.id" :item="item"
            @edit-journal="(entry) => emit('edit-journal', entry)" />
        </TransitionGroup>
      </section>
    </template>

    <ReflectionDialog v-if="showReflection" @close="showReflection = false" />
  </div>
</template>

<style scoped>
.journal-view {
  padding: 0 12px 12px;
}

.journal-view__header {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 0.5rem;
  padding-bottom: 0.25rem;
  position: relative;
  min-height: 3rem;
}

.journal-view__header .ds-page-header {
  position: absolute;
  left: 0;
  right: 0;
  pointer-events: none;
  line-height: 1.2;
}

.journal-view__actions {
  display: flex;
  align-items: center;
  gap: 0;
  flex-shrink: 0;
  margin-left: auto;
  position: relative;
  z-index: 1;
}

.journal-view__action-btn {
  width: 2.5rem;
  height: 2.5rem;
  flex-shrink: 0;
}

.journal-view__search-bar {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 0 0.75rem;
}

.journal-view__search-icon {
  color: var(--p-text-muted-color);
  font-size: 0.9375rem;
}

.journal-view__search-input {
  flex: 1;
  min-width: 0;
}

.journal-search-slide-enter-active,
.journal-search-slide-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.journal-search-slide-enter-from,
.journal-search-slide-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}

.journal-view__filter-bar {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  padding: 0.75rem 0;
  border-top: 1px solid var(--p-content-border-color);
}

.journal-view__filter-label {
  font-size: 0.8125rem;
  font-weight: 500;
  color: var(--p-text-color);
}

.journal-view__filter-dropdown {
  width: 100%;
}

.journal-view__filter-clear {
  align-self: flex-start;
  margin-top: 0.25rem;
}

.journal-skeleton {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.journal-section {
  margin-bottom: 0.5rem;
  position: relative;
}

.journal-section__header {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  font-size: 0.8125rem;
  font-weight: 600;
  color: var(--p-text-muted-color);
  text-transform: uppercase;
  letter-spacing: 0.04em;
  padding: 0.25rem 0.25rem 0.5rem;
}

.journal-section__icon {
  font-size: 0.6875rem;
}

.journal-cards {
  display: flex;
  flex-direction: column;
  gap: 0.625rem;
}

.journal-list-move {
  transition: transform 0.4s ease;
}

.journal-list-enter-active {
  transition: all 0.35s ease;
}

.journal-list-leave-active {
  transition: all 0.3s ease;
  position: absolute;
  width: 100%;
}

.journal-list-enter-from {
  opacity: 0;
  transform: translateY(-16px);
}

.journal-list-leave-to {
  opacity: 0;
  transform: scale(0.95);
}
</style>
