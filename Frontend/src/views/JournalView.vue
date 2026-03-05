<script setup lang="ts">
import EmptyState from '@/components/EmptyState.vue';
import JournalCard from '@/components/JournalCard.vue';
import Skeleton from 'primevue/skeleton';
import { useJournalStore } from '@/stores/journal';
import { computed, onMounted, ref, nextTick } from 'vue';
import type { JournalEntryDTO } from '@/api/JournalAPI';

const journalStore = useJournalStore();

const pinnedItems = computed(() => journalStore.pinnedEntries);
const regularItems = computed(() => journalStore.regularEntries);
const hasEntries = computed(() => journalStore.entries.length > 0);

const emit = defineEmits<{
  (e: 'edit-journal', entry: JournalEntryDTO | null): void
}>();

onMounted(async () => {
  await journalStore.loadEntries();
});
</script>

<template>
  <div class="journal-view">
    <h1 class="view-page-header">{{ $t('journal.title') }}</h1>

    <div v-if="journalStore.isLoading" class="journal-skeleton">
      <div v-for="i in 3" :key="i" class="skeleton-card">
        <Skeleton width="100%" height="4.5rem" borderRadius="16px" />
      </div>
    </div>

    <EmptyState v-else-if="!hasEntries" icon="pi pi-book" :title="$t('journal.empty')"
      :subtitle="$t('journal.emptySubtitle')" />

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
  </div>
</template>

<style scoped>
.journal-view {
  padding: 0 12px 12px;
}

.view-page-header {
  font-size: var(--p-card-title-font-size);
  font-weight: 600;
  text-align: center;
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
