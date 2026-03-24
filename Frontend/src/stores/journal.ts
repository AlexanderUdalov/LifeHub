import { defineStore } from 'pinia';
import { computed, ref } from 'vue';
import {
    type CreateJournalEntryRequest,
    type JournalEntryDTO,
    type UpdateJournalEntryPayload,
    createJournalEntry,
    deleteJournalEntry,
    getJournalEntries,
    updateJournalEntry
} from '@/api/JournalAPI';

export const useJournalStore = defineStore('journal', () => {
    const entries = ref<JournalEntryDTO[]>([]);
    const isLoading = ref(false);
    const error = ref<string | null>(null);

    const pinnedEntries = computed(() => entries.value.filter((e) => e.isPinned));
    const regularEntries = computed(() => entries.value.filter((e) => !e.isPinned));

    async function loadEntries() {
        isLoading.value = true;
        error.value = null;
        try {
            entries.value = await getJournalEntries();
        } catch (e) {
            error.value = e instanceof Error ? e.message : String(e);
            throw e;
        } finally {
            isLoading.value = false;
        }
    }

    async function createEntry(request: CreateJournalEntryRequest) {
        const created = await createJournalEntry(request);
        entries.value.unshift(created);
        return created;
    }

    async function editEntry(id: string, request: UpdateJournalEntryPayload) {
        const updated = await updateJournalEntry(id, request);
        const idx = entries.value.findIndex((e) => e.id === updated.id);
        if (idx !== -1) {
            entries.value[idx] = updated;
        }
        return updated;
    }

    async function removeEntry(id: string) {
        await deleteJournalEntry(id);
        entries.value = entries.value.filter((e) => e.id !== id);
    }

    async function togglePin(id: string) {
        const existing = entries.value.find((e) => e.id === id);
        if (!existing) return;
        const updated = await updateJournalEntry(id, {
            isPinned: !existing.isPinned,
            taskItemId: existing.taskItemId ?? null,
            habitId: existing.habitId ?? null,
            addictionId: existing.addictionId ?? null,
            goalId: existing.goalId ?? null,
            lifeAreaId: existing.lifeAreaId ?? null
        });
        const idx = entries.value.findIndex((e) => e.id === updated.id);
        if (idx !== -1) {
            entries.value[idx] = updated;
        }
        return updated;
    }

    return {
        entries,
        isLoading,
        error,
        pinnedEntries,
        regularEntries,
        loadEntries,
        createEntry,
        editEntry,
        removeEntry,
        togglePin
    };
});

