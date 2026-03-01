<script setup lang="ts">
import EmptyState from '@/components/EmptyState.vue';
import JournalCard from '@/components/JournalCard.vue';
import Skeleton from 'primevue/skeleton';
import { useJournalStore } from '@/stores/journal';
import { computed, onMounted } from 'vue';
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
      <div v-for="i in 3" :key="i" class="skeleton-block">
        <Skeleton width="50%" height="1.25rem" class="skeleton-title" />
        <Skeleton width="100%" height="3rem" />
      </div>
    </div>

    <EmptyState v-else-if="!hasEntries" icon="pi pi-book"
      :title="$t('journal.empty')" :subtitle="$t('journal.emptySubtitle')" />

    <template v-else>
      <div v-if="pinnedItems.length">
        <h3>Pinned</h3>
        <JournalCard v-for="item in pinnedItems" :key="item.id" :item="item" @edit-journal="(entry) => emit('edit-journal', entry)" />
      </div>

      <div>
        <JournalCard v-for="item in regularItems" :key="item.id" :item="item" @edit-journal="(entry) => emit('edit-journal', entry)" />
      </div>
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
  gap: 1rem;
}

.skeleton-block {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.skeleton-title {
  margin-bottom: 0.25rem;
}

</style>
