<script setup lang="ts">
import Card from 'primevue/card'
import Dialog from 'primevue/dialog'
import Button from 'primevue/button'
import { computed, ref } from 'vue'
import type { JournalEntryDTO } from '@/api/JournalAPI'
import { useJournalStore } from '@/stores/journal'

const props = defineProps<{ item: JournalEntryDTO }>()

const emit = defineEmits<{
  (e: 'edit-journal', entry: JournalEntryDTO): void
}>()

const journalStore = useJournalStore()

const showDeleteDialog = ref(false)

const formattedDate = computed(() =>
  new Date(props.item.createdAt).toLocaleDateString('ru-RU', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  })
)

const isPinned = computed(() => props.item.isPinned)

async function onTogglePin() {
  await journalStore.togglePin(props.item.id)
}

async function onConfirmDelete() {
  await journalStore.removeEntry(props.item.id)
  showDeleteDialog.value = false
}
</script>

<template>
  <Card>
    <template #title>
      <div>
        <div>
          📅 {{ formattedDate }}
        </div>
        <div>
          <Button :icon="isPinned ? 'pi pi-thumbtack' : 'pi pi-thumbtack'" variant="text" rounded size="small"
            :severity="isPinned ? 'secondary' : undefined" @click="onTogglePin" />
          <Button icon="pi pi-pencil" variant="text" rounded size="small"
            @click="emit('edit-journal', item)" />
          <Button icon="pi pi-trash" variant="text" rounded size="small" severity="danger"
            @click="showDeleteDialog = true" />
        </div>
      </div>
    </template>

    <template #content>
      <p>
        {{ item.text }}
      </p>
    </template>
  </Card>

  <Dialog v-model:visible="showDeleteDialog" header="Delete note" modal>
    <p>Are you sure you want to delete this note?</p>
    <template #footer>
      <Button label="Cancel" text @click="showDeleteDialog = false" />
      <Button label="Delete" severity="danger" @click="onConfirmDelete" />
    </template>
  </Dialog>
</template>
