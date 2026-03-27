<script setup lang="ts">
import Button from 'primevue/button'
import Textarea from 'primevue/textarea'
import Select from 'primevue/select'
import Message from 'primevue/message'
import { computed, onMounted, ref, watch, nextTick } from 'vue'
import { useJournalStore } from '@/stores/journal'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { useGoalsStore } from '@/stores/goals'
import type { JournalEntryDTO } from '@/api/JournalAPI'
import { useApiError } from '@/composables/useApiError'
import { useI18n } from 'vue-i18n'
import BaseDrawer from '@/components/base/BaseDrawer.vue'

const { t, locale } = useI18n()

const props = defineProps<{
  entry: JournalEntryDTO | null
}>()

const emit = defineEmits<{
  (e: 'close'): void
}>()

const journalStore = useJournalStore()
const lifeAreasStore = useLifeAreasStore()
const goalsStore = useGoalsStore()
const apiError = useApiError()

const visible = ref(true)
watch(visible, (v) => {
  if (!v) emit('close')
})

const localText = ref(props.entry?.text ?? '')
const localPinned = ref(props.entry?.isPinned ?? false)
const localLifeAreaId = ref<string | null>(props.entry?.lifeAreaId ?? null)
const localGoalId = ref<string | null>(props.entry?.goalId ?? null)

const lifeAreaChipLabel = computed(() => {
  if (!localLifeAreaId.value) return t('lifeareas.field')
  const area = lifeAreasStore.lifeAreas.find(a => a.id === localLifeAreaId.value)
  return area?.name ?? t('lifeareas.field')
})

const goalChipLabel = computed(() => {
  if (!localGoalId.value) return t('goals.field')
  const g = goalsStore.getGoalById(localGoalId.value)
  return g?.title ?? t('goals.field')
})

const hasLifeArea = computed(() => !!localLifeAreaId.value)
const hasGoal = computed(() => !!localGoalId.value)

const isEdit = computed(() => !!props.entry)
const canSave = computed(() => localText.value.trim().length > 0)

const isSaveLoading = ref(false)
const errorText = ref('')

const textareaWrap = ref<HTMLElement | null>(null)

const MIN_ROWS = 6

function resizeTextarea() {
  nextTick(() => {
    const el = textareaWrap.value?.querySelector?.('textarea')
    if (!el) return
    el.style.height = '0'
    const lineHeight = parseFloat(getComputedStyle(el).lineHeight) || 22
    const minHeight = MIN_ROWS * lineHeight
    el.style.height = `${Math.max(minHeight, el.scrollHeight)}px`
  })
}

watch(localText, () => resizeTextarea(), { flush: 'post' })

onMounted(() => {
  resizeTextarea()
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
        lifeAreaId: localLifeAreaId.value,
        goalId: localGoalId.value
      })
    } else {
      await journalStore.createEntry({
        text: localText.value,
        taskItemId: null,
        habitId: null,
        addictionId: null,
        goalId: localGoalId.value,
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
  <BaseDrawer v-model:visible="visible" class="journal-drawer">
    <template #header>
      <span class="journal-drawer-title">
        {{ isEdit ? t('journal.editNote') : t('journal.newNote') }}
      </span>
    </template>

    <div ref="textareaWrap">
      <Textarea
        v-model="localText"
        :placeholder="t('journal.placeholder')"
        autoResize
        class="journal-drawer-textarea"
        :rows="MIN_ROWS"
        fluid
      />
      <span class="journal-drawer-markdown-hint">{{ t('journal.markdownHint') }}</span>
    </div>

    <div class="ds-chip-row">
      <Select v-model="localLifeAreaId" :options="lifeAreasStore.lifeAreas" option-label="name" option-value="id"
        show-clear :placeholder="t('lifeareas.field')" class="ds-chip-select"
        :class="{ 'ds-chip-select--active': hasLifeArea }">
        <template #value>
          <span class="ds-chip-select-value">
            <i class="pi pi-chart-pie" />
            {{ lifeAreaChipLabel }}
          </span>
        </template>
      </Select>
      <Select v-model="localGoalId" :options="goalsStore.goalsSorted" option-label="title" option-value="id"
        show-clear :placeholder="t('goals.field')" class="ds-chip-select"
        :class="{ 'ds-chip-select--active': hasGoal }">
        <template #value>
          <span class="ds-chip-select-value">
            <i class="pi pi-bullseye" />
            {{ goalChipLabel }}
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
      <div class="ds-drawer-actions">
        <span />
        <Button :label="isEdit ? t('journal.save') : t('journal.create')" :disabled="!canSave" :loading="isSaveLoading"
          icon="pi pi-check" @click="onSave" />
      </div>
    </template>
  </BaseDrawer>
</template>

<style>

.journal-drawer-title {
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--p-text-color);
}

.journal-drawer-textarea.p-textarea {
  border: none;
  box-shadow: none !important;
  background: transparent;
  font-size: 1rem;
  line-height: 1.6;
  padding: 0.5rem 0;
}

.journal-drawer-markdown-hint {
  display: block;
  font-size: 0.75rem;
  color: var(--p-text-muted-color);
  margin-top: 0.25rem;
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

</style>
