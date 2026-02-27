<script setup lang="ts">
import JournalCard from '@/components/JournalCard.vue';
import { useJournalStore } from '@/stores/journal';
import { computed, onMounted } from 'vue';
import type { JournalEntryDTO } from '@/api/JournalAPI';

const journalStore = useJournalStore();

const pinnedItems = computed(() => journalStore.pinnedEntries);
const regularItems = computed(() => journalStore.regularEntries);

const emit = defineEmits<{
  (e: 'edit-journal', entry: JournalEntryDTO | null): void
}>();

onMounted(async () => {
  await journalStore.loadEntries();
});
</script>

<template>
  <div v-if="pinnedItems.length">
    <h3>Pinned</h3>
    <JournalCard v-for="item in pinnedItems" :key="item.id" :item="item" @edit-journal="(entry) => emit('edit-journal', entry)" />
  </div>

  <div>
    <JournalCard v-for="item in regularItems" :key="item.id" :item="item" @edit-journal="(entry) => emit('edit-journal', entry)" />
  </div>
</template>
