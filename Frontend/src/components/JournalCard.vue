<script setup lang="ts">
import Card from 'primevue/card'
import Button from 'primevue/button'
import { computed, ref } from 'vue'
import type { JournalEntryDTO } from '@/api/JournalAPI'
import { useJournalStore } from '@/stores/journal'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { useI18n } from 'vue-i18n'

const props = defineProps<{ item: JournalEntryDTO }>()

const emit = defineEmits<{
  (e: 'edit-journal', entry: JournalEntryDTO): void
}>()

const { t, locale } = useI18n()
const journalStore = useJournalStore()
const lifeAreasStore = useLifeAreasStore()

const lifeArea = computed(() => {
  if (!props.item.lifeAreaId) return null
  return lifeAreasStore.lifeAreas.find(a => a.id === props.item.lifeAreaId) ?? null
})

const isExpanded = ref(false)
const showDeleteConfirm = ref(false)

const formattedDate = computed(() => {
  const date = new Date(props.item.createdAt)
  const now = new Date()
  const isToday = date.toDateString() === now.toDateString()

  const yesterday = new Date(now)
  yesterday.setDate(yesterday.getDate() - 1)
  const isYesterday = date.toDateString() === yesterday.toDateString()

  if (isToday) {
    return date.toLocaleTimeString(locale.value, { hour: '2-digit', minute: '2-digit' })
  }
  if (isYesterday) {
    return t('tasks.dates.yesterday')
  }
  return date.toLocaleDateString(locale.value, {
    day: 'numeric',
    month: 'short',
    ...(date.getFullYear() !== now.getFullYear() && { year: 'numeric' })
  })
})

const wasEdited = computed(() => !!props.item.updatedAt)

async function onTogglePin() {
  await journalStore.togglePin(props.item.id)
}

async function onConfirmDelete() {
  await journalStore.removeEntry(props.item.id)
  showDeleteConfirm.value = false
}
</script>

<template>
  <Card class="journal-card" :class="{ 'journal-card--pinned': item.isPinned }">
    <template #content>
      <div class="journal-card__header">
        <div class="journal-card__meta">
          <i v-if="item.isPinned" class="pi pi-thumbtack journal-card__pin-badge" />
          <span class="journal-card__date">{{ formattedDate }}</span>
          <span v-if="wasEdited" class="journal-card__edited">{{ t('journal.edited') }}</span>
        </div>

        <div class="journal-card__actions" @click.stop>
          <Button
            icon="pi pi-thumbtack"
            variant="text"
            rounded
            size="small"
            :severity="item.isPinned ? undefined : 'secondary'"
            class="journal-card__action-btn"
            @click="onTogglePin"
          />
          <Button
            icon="pi pi-pencil"
            variant="text"
            rounded
            size="small"
            severity="secondary"
            class="journal-card__action-btn"
            @click="emit('edit-journal', item)"
          />
          <Button
            icon="pi pi-trash"
            variant="text"
            rounded
            size="small"
            severity="danger"
            class="journal-card__action-btn"
            @click="showDeleteConfirm = true"
          />
        </div>
      </div>

      <div class="journal-card__body" @click="emit('edit-journal', item)">
        <p
          class="journal-card__text"
          :class="{ 'journal-card__text--expanded': isExpanded }"
          @click.stop="isExpanded = !isExpanded"
        >
          {{ item.text }}
        </p>
      </div>

      <div v-if="lifeArea" class="journal-card__life-area">
        <i class="pi pi-chart-pie" />
        <span>{{ lifeArea.name }}</span>
      </div>

      <Transition name="journal-card-slide">
        <div v-if="showDeleteConfirm" class="journal-card__confirm" @click.stop>
          <span class="journal-card__confirm-text">{{ t('journal.deleteConfirm') }}</span>
          <div class="journal-card__confirm-btns">
            <Button :label="t('cancel')" variant="text" size="small" @click="showDeleteConfirm = false" />
            <Button :label="t('journal.delete')" severity="danger" size="small" @click="onConfirmDelete" />
          </div>
        </div>
      </Transition>
    </template>
  </Card>
</template>

<style scoped>
.journal-card {
  border-radius: 16px;
  overflow: hidden;
  transition: box-shadow 0.2s ease, transform 0.15s ease;
  position: relative;
}

.journal-card:active {
  transform: scale(0.985);
}

.journal-card--pinned {
  border-left: 3px solid var(--p-primary-color);
}

:deep(.p-card-body) {
  padding: 1rem 1.25rem;
}

:deep(.p-card-content) {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.journal-card__body {
  cursor: pointer;
}

.journal-card__text {
  margin: 0;
  font-size: 0.9375rem;
  line-height: 1.65;
  color: var(--p-text-color);
  white-space: pre-wrap;
  word-break: break-word;
  display: -webkit-box;
  -webkit-line-clamp: 5;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.journal-card__text--expanded {
  -webkit-line-clamp: unset;
  overflow: visible;
}

.journal-card__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  padding-bottom: 0.625rem;
  border-bottom: 1px solid var(--p-content-border-color);
}

.journal-card__meta {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  font-size: 0.8125rem;
  color: var(--p-text-muted-color);
  min-width: 0;
}

.journal-card__pin-badge {
  font-size: 0.6875rem;
  color: var(--p-primary-color);
}

.journal-card__date {
  white-space: nowrap;
}

.journal-card__edited {
  opacity: 0.65;
  white-space: nowrap;
}

.journal-card__edited::before {
  content: '·';
  margin-right: 0.375rem;
}

.journal-card__actions {
  display: flex;
  gap: 0;
  flex-shrink: 0;
}

.journal-card__action-btn {
  width: 2rem;
  height: 2rem;
}

.journal-card__life-area {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  font-size: 0.75rem;
  color: var(--p-text-muted-color);
  padding: 0.25rem 0;
}

.journal-card__life-area i {
  font-size: 0.625rem;
}

.journal-card__confirm {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  padding-top: 0.75rem;
  border-top: 1px solid var(--p-content-border-color);
}

.journal-card__confirm-text {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--p-text-color);
}

.journal-card__confirm-btns {
  display: flex;
  gap: 0.25rem;
  flex-shrink: 0;
}

.journal-card-slide-enter-active,
.journal-card-slide-leave-active {
  transition: all 0.2s ease;
}

.journal-card-slide-enter-from,
.journal-card-slide-leave-to {
  opacity: 0;
  transform: translateY(8px);
}
</style>
