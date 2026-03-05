<script setup lang="ts">
import Drawer from 'primevue/drawer'
import Button from 'primevue/button'
import Textarea from 'primevue/textarea'
import Select from 'primevue/select'
import Message from 'primevue/message'
import { computed, onMounted, ref, watch } from 'vue'
import { useJournalStore } from '@/stores/journal'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import type { JournalEntryDTO } from '@/api/JournalAPI'
import { useApiError } from '@/composables/useApiError'
import { useI18n } from 'vue-i18n'

const { t, locale } = useI18n()

const props = defineProps<{
  entry: JournalEntryDTO | null
}>()

const emit = defineEmits<{
  (e: 'close'): void
}>()

const journalStore = useJournalStore()
const lifeAreasStore = useLifeAreasStore()
const apiError = useApiError()

const visible = ref(true)
watch(visible, (v) => {
  if (!v) emit('close')
})

const localText = ref(props.entry?.text ?? '')
const localPinned = ref(props.entry?.isPinned ?? false)
const localLifeAreaId = ref<string | null>(props.entry?.lifeAreaId ?? null)

const lifeAreaChipLabel = computed(() => {
  if (!localLifeAreaId.value) return t('lifeareas.field')
  const area = lifeAreasStore.lifeAreas.find(a => a.id === localLifeAreaId.value)
  return area?.name ?? t('lifeareas.field')
})

const hasLifeArea = computed(() => !!localLifeAreaId.value)

const isEdit = computed(() => !!props.entry)
const canSave = computed(() => localText.value.trim().length > 0)

const isSaveLoading = ref(false)
const errorText = ref('')

const textareaWrap = ref<HTMLElement | null>(null)

onMounted(() => {
  setTimeout(() => {
    textareaWrap.value?.querySelector('textarea')?.focus()
  }, 300)
})

async function onSave() {
  if (!canSave.value) return

  errorText.value = ''
  isSaveLoading.value = true

  try {
    if (isEdit.value && props.entry) {
      await journalStore.editEntry(props.entry.id, {
        text: localText.value,
        createdAt: props.entry.createdAt,
        isPinned: localPinned.value,
        lifeAreaId: localLifeAreaId.value
      })
    } else {
      await journalStore.createEntry({
        text: localText.value,
        taskItemId: null,
        habitId: null,
        addictionId: null,
        goalId: null,
        lifeAreaId: localLifeAreaId.value
      })
    }

    emit('close')
  } catch (e) {
    errorText.value = apiError.resolveMessage(e)
  } finally {
    isSaveLoading.value = false
  }
}

const formattedDate = computed(() => {
  if (!props.entry) return ''
  return new Date(props.entry.createdAt).toLocaleDateString(locale.value, {
    day: 'numeric',
    month: 'long',
    year: 'numeric'
  })
})
</script>

<template>
  <Drawer v-model:visible="visible" position="bottom" class="journal-drawer" style="height: auto; max-height: 85vh">
    <template #header>
      <span class="journal-drawer-title">
        {{ isEdit ? t('journal.editNote') : t('journal.newNote') }}
      </span>
    </template>

    <div ref="textareaWrap">
      <Textarea v-model="localText" :placeholder="t('journal.placeholder')" autoResize class="journal-drawer-textarea"
        :rows="6" fluid />
    </div>

    <div class="journal-drawer-chips">
      <Select v-model="localLifeAreaId" :options="lifeAreasStore.lifeAreas" option-label="name" option-value="id"
        show-clear :placeholder="t('lifeareas.field')" class="journal-chip-select"
        :class="{ 'journal-chip-select--active': hasLifeArea }">
        <template #value>
          <span class="journal-chip-select-value">
            <i class="pi pi-chart-pie" />
            {{ lifeAreaChipLabel }}
          </span>
        </template>
      </Select>
    </div>

    <div v-if="isEdit" class="journal-drawer-meta">
      <Button icon="pi pi-thumbtack" :label="localPinned ? t('journal.pinnedEntry') : t('journal.pin')"
        :severity="localPinned ? undefined : 'secondary'" :variant="localPinned ? 'outlined' : 'text'" size="small"
        @click="localPinned = !localPinned" />
      <span class="journal-drawer-date">
        <i class="pi pi-calendar" />
        {{ formattedDate }}
      </span>
    </div>

    <Message v-if="errorText.length" severity="error" icon="pi pi-times-circle" :life="3000">
      {{ errorText }}
    </Message>

    <template #footer>
      <div class="journal-drawer-actions">
        <span />
        <Button :label="isEdit ? t('journal.save') : t('journal.create')" :disabled="!canSave" :loading="isSaveLoading"
          icon="pi pi-check" @click="onSave" />
      </div>
    </template>
  </Drawer>
</template>

<style>
.p-drawer.journal-drawer {
  border-radius: 1rem 1rem 0 0;
}

.journal-drawer .p-drawer-header {
  position: relative;
  padding: 0.75rem 1.25rem;
  padding-top: 1.5rem;
}

.journal-drawer .p-drawer-header::before {
  content: '';
  position: absolute;
  top: 0.5rem;
  left: 50%;
  transform: translateX(-50%);
  width: 2.5rem;
  height: 0.25rem;
  background: var(--p-content-border-color);
  border-radius: 999px;
}

.journal-drawer-title {
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--p-text-color);
}

.journal-drawer .p-drawer-content {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding-bottom: 0.25rem;
}

.journal-drawer-textarea.p-textarea {
  border: none;
  box-shadow: none !important;
  background: transparent;
  font-size: 1rem;
  line-height: 1.6;
  padding: 0.5rem 0;
}

.journal-drawer-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  padding: 0.5rem 0;
  border-top: 1px solid var(--p-content-border-color);
}

.journal-chip-select.p-select {
  border-radius: 999px;
  border-color: transparent;
  background: transparent;
  box-shadow: none;
  font-size: 0.8125rem;
  height: auto;
  padding-right: 0.5rem;
}

.journal-chip-select.p-select .p-select-label {
  display: flex;
  align-items: center;
  padding: 0.25rem 0.5rem;
  font-size: 0.8125rem;
  color: var(--p-text-muted-color);
}

.journal-chip-select.p-select .p-select-dropdown {
  display: none;
}

.journal-chip-select--active.p-select {
  border-color: var(--p-primary-color);
}

.journal-chip-select--active.p-select .p-select-label {
  color: var(--p-primary-color);
}

.journal-chip-select-value {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  white-space: nowrap;
}

.journal-chip-select-value i {
  font-size: 0.75rem;
}

.journal-drawer-meta {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.5rem 0;
  border-top: 1px solid var(--p-content-border-color);
}

.journal-drawer-date {
  font-size: 0.8125rem;
  color: var(--p-text-muted-color);
  display: flex;
  align-items: center;
  gap: 0.375rem;
}

.journal-drawer-date i {
  font-size: 0.75rem;
}

.journal-drawer .p-drawer-footer {
  border-top: 1px solid var(--p-content-border-color);
}

.journal-drawer-actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
}
</style>
