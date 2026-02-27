<script setup lang="ts">
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Textarea from 'primevue/textarea';
import Message from 'primevue/message';
import { computed, ref } from 'vue';
import { useJournalStore } from '@/stores/journal';
import type { JournalEntryDTO } from '@/api/JournalAPI';
import { useApiError } from '@/composables/useApiError';

const props = defineProps<{
  entry: JournalEntryDTO | null;
}>();

const emit = defineEmits<{
  (e: 'close'): void;
}>();

const journalStore = useJournalStore();
const apiError = useApiError();

const localText = ref(props.entry?.text ?? '');
const localDate = ref<Date>(props.entry ? new Date(props.entry.createdAt) : new Date());
const localPinned = ref<boolean>(props.entry?.isPinned ?? false);

const isEdit = computed(() => !!props.entry);
const canSave = computed(() => localText.value.trim().length > 0);

const isSaveLoading = ref(false);
const isDeleteLoading = ref(false);
const errorText = ref('');

async function onSave() {
  if (!canSave.value) return;

  errorText.value = '';
  isSaveLoading.value = true;

  try {
    if (isEdit.value && props.entry) {
      await journalStore.editEntry(props.entry.id, {
        text: localText.value,
        createdAt: localDate.value.toISOString(),
        isPinned: localPinned.value
      });
    } else {
      await journalStore.createEntry({
        text: localText.value,
        taskItemId: null,
        habitId: null,
        addictionId: null,
        goalId: null,
        lifeAreaId: null
      });
    }

    emit('close');
  } catch (e) {
    errorText.value = apiError.resolveMessage(e);
  } finally {
    isSaveLoading.value = false;
  }
}

async function onDelete() {
  if (!isEdit.value || !props.entry) return;

  errorText.value = '';
  isDeleteLoading.value = true;

  try {
    await journalStore.removeEntry(props.entry.id);
    emit('close');
  } catch (e) {
    errorText.value = apiError.resolveMessage(e);
  } finally {
    isDeleteLoading.value = false;
  }
}
</script>

<template>
  <Dialog modal :visible="true" :draggable="false" :header="isEdit ? 'Edit note' : 'New note'"
    style="width: 90vw; max-width: 420px" @hide="emit('close')">
    <div>
      <label>Text</label>
      <Textarea v-model="localText" rows="6" autoResize class="w-full" />
    </div>

    <Message v-if="errorText.length" severity="error" icon="pi pi-times-circle" :life="3000">
      {{ errorText }}
    </Message>

    <template #footer>
      <Button v-if="isEdit" label="Delete" severity="danger" :loading="isDeleteLoading" @click="onDelete" />
      <Button :label="isEdit ? 'Save' : 'Create'" :disabled="!canSave" :loading="isSaveLoading" @click="onSave" />
    </template>
  </Dialog>
</template>
