<script setup lang="ts">
import Button from 'primevue/button'
import { computed, ref, onMounted, onUnmounted } from 'vue'
import { useClampedContent } from '@/composables/useClampedContent'
import type { JournalEntryDTO } from '@/api/JournalAPI'
import { useJournalStore } from '@/stores/journal'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { useGoalsStore } from '@/stores/goals'
import { useAddictionsStore } from '@/stores/addictions'
import { useI18n } from 'vue-i18n'
import { renderMarkdown } from '@/composables/useMarkdown'
import BaseCard from '@/components/base/BaseCard.vue'

const props = withDefaults(defineProps<{ item: JournalEntryDTO; noBorder?: boolean }>(), {
  noBorder: false,
})

const emit = defineEmits<{
  (e: 'edit-journal', entry: JournalEntryDTO): void
}>()

const { t, locale } = useI18n()
const journalStore = useJournalStore()
const lifeAreasStore = useLifeAreasStore()
const goalsStore = useGoalsStore()
const addictionsStore = useAddictionsStore()
const areaColor = computed(() => lifeAreasStore.getAreaColorById(props.item.lifeAreaId))

const lifeArea = computed(() => {
  if (!props.item.lifeAreaId) return null
  return lifeAreasStore.lifeAreas.find((a) => a.id === props.item.lifeAreaId) ?? null
})

const goal = computed(() => goalsStore.getGoalById(props.item.goalId))

const addiction = computed(() => {
  if (!props.item.addictionId) return null
  return (
    addictionsStore.addictions.find((a) => a.addiction.id === props.item.addictionId)?.addiction ??
    null
  )
})

const addictionLabel = computed(() => addiction.value?.title ?? t('journal.deletedAddiction'))
const lifeAreaLabel = computed(() => lifeArea.value?.name ?? '')
const goalLabel = computed(() => goal.value?.title ?? '')

const hasFooterMeta = computed(
  () => !!props.item.addictionId || !!lifeArea.value || !!goal.value || props.item.aiGenerated,
)

const textRef = ref<HTMLElement | null>(null)
const menuRef = ref<HTMLDetailsElement | null>(null)
const { expanded: isExpanded, foldable, toggle } = useClampedContent(textRef, () => props.item.text)
const closeMenu = () => {
  if (menuRef.value) menuRef.value.open = false
}
const outsideClick = (event: MouseEvent) => {
  if (event.target instanceof Node && !menuRef.value?.contains(event.target)) closeMenu()
}
onMounted(() => document.addEventListener('click', outsideClick))
onUnmounted(() => {
  document.removeEventListener('click', outsideClick)
  if (copyResetTimer) clearTimeout(copyResetTimer)
})
const showDeleteConfirm = ref(false)
const copySucceeded = ref(false)
let copyResetTimer: ReturnType<typeof setTimeout> | null = null

function fallbackCopyToClipboard(text: string) {
  const ta = document.createElement('textarea')
  ta.value = text
  ta.setAttribute('readonly', '')
  ta.style.position = 'fixed'
  ta.style.left = '-9999px'
  document.body.appendChild(ta)
  ta.select()
  document.execCommand('copy')
  document.body.removeChild(ta)
}

async function copyEntryText() {
  const text = props.item.text ?? ''
  const markSuccess = () => {
    copySucceeded.value = true
    if (copyResetTimer) clearTimeout(copyResetTimer)
    copyResetTimer = setTimeout(() => {
      copySucceeded.value = false
      copyResetTimer = null
    }, 1500)
  }
  try {
    if (navigator.clipboard?.writeText) {
      await navigator.clipboard.writeText(text)
    } else {
      fallbackCopyToClipboard(text)
    }
    markSuccess()
  } catch {
    try {
      fallbackCopyToClipboard(text)
      markSuccess()
    } catch {
      // Clipboard unavailable (permissions / non-secure context)
    }
  }
}

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
    ...(date.getFullYear() !== now.getFullYear() && { year: 'numeric' }),
  })
})

const renderedText = computed(() => renderMarkdown(props.item.text))
const containsEmoji = (value: string) => /\p{Extended_Pictographic}/u.test(value)
const showAddictionIcon = computed(() => !containsEmoji(addictionLabel.value))
const showLifeAreaIcon = computed(() => !containsEmoji(lifeAreaLabel.value))
const showGoalIcon = computed(() => !containsEmoji(goalLabel.value))

async function onTogglePin() {
  await journalStore.togglePin(props.item.id)
}

async function onConfirmDelete() {
  await journalStore.removeEntry(props.item.id)
  showDeleteConfirm.value = false
}
</script>

<template>
  <BaseCard class="journal-card" :borderless="noBorder" :accent-color="areaColor">
    <template #content>
      <div class="journal-card__header">
        <div class="journal-card__meta">
          <span>{{ formattedDate }}</span
          ><i
            aria-hidden="true"
            v-if="item.isPinned"
            class="pi pi-thumbtack"
            :aria-label="t('journal.pinnedEntry')"
          />
        </div>
        <div class="journal-card__header-right">
          <span
            v-if="lifeArea"
            class="journal-area"
            :style="{ '--area-color': areaColor ?? 'var(--app-muted)' }"
            ><span />{{ lifeAreaLabel }}</span
          >
          <details ref="menuRef" class="journal-menu" @keydown.esc.prevent="closeMenu">
            <summary :aria-label="t('redesign.entryActions')">
              <i aria-hidden="true" class="pi pi-ellipsis-h" />
            </summary>
            <div class="journal-menu-items" @click="closeMenu">
              <button type="button" @click="emit('edit-journal', item)">
                <i aria-hidden="true" class="pi pi-pencil" />{{ t('journal.editNote') }}</button
              ><button type="button" @click="onTogglePin">
                <i aria-hidden="true" class="pi pi-thumbtack" />{{
                  t(item.isPinned ? 'redesign.unpin' : 'journal.pin')
                }}</button
              ><button type="button" @click="copyEntryText">
                <i aria-hidden="true" :class="copySucceeded ? 'pi pi-check' : 'pi pi-copy'" />{{
                  t('journal.copyText')
                }}</button
              ><button type="button" class="danger" @click="showDeleteConfirm = true">
                <i aria-hidden="true" class="pi pi-trash" />{{ t('journal.delete') }}
              </button>
            </div>
          </details>
        </div>
      </div>
      <div v-if="showDeleteConfirm" class="journal-card__confirm">
        <span>{{ t('journal.deleteConfirm') }}</span>
        <div>
          <Button
            :label="t('cancel')"
            variant="text"
            size="small"
            @click="showDeleteConfirm = false"
          /><Button
            :label="t('journal.delete')"
            severity="danger"
            size="small"
            @click="onConfirmDelete"
          />
        </div>
      </div>
      <div class="journal-card__text" :class="{ folded: foldable && !isExpanded }" @click="toggle">
        <div ref="textRef" class="journal-card__text-inner" v-html="renderedText" />
      </div>
      <button
        v-if="foldable"
        type="button"
        class="text-action journal-fold"
        :aria-expanded="isExpanded"
        @click="toggle()"
      >
        {{ t(isExpanded ? 'redesign.collapse' : 'redesign.expand')
        }}<i aria-hidden="true" :class="isExpanded ? 'pi pi-angle-up' : 'pi pi-angle-down'" />
      </button>
      <div v-if="item.addictionId || goal || item.aiGenerated" class="journal-card__footer">
        <RouterLink v-if="item.addictionId" :to="'/addictions#tracker-' + item.addictionId"
          ><i aria-hidden="true" class="pi pi-heart" />{{ addictionLabel }}</RouterLink
        ><RouterLink v-if="goal" to="/goals"
          ><i aria-hidden="true" class="pi pi-flag" />{{ goalLabel }}</RouterLink
        ><span v-if="item.aiGenerated"
          ><i aria-hidden="true" class="pi pi-sparkles" />{{ t('journal.aiGeneratedChip') }}</span
        >
      </div>
    </template>
  </BaseCard>
</template>
<style scoped>
.journal-card {
  position: relative;
  border-radius: var(--ds-radius-lg);
  margin-bottom: 10px;
}
.journal-card :deep(.p-card-body) {
  padding: 12px 14px;
  gap: 0;
}
.journal-card :deep(.p-card-content) {
  display: flex;
  flex-direction: column;
  gap: 0;
}
.journal-card__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 8px;
}
.journal-card__meta {
  display: flex;
  align-items: center;
  gap: 7px;
  font-size: 11px;
  color: var(--app-muted);
  min-width: 0;
}
.journal-card__meta i {
  font-size: 10px;
  color: var(--app-accent);
}
.journal-card__header-right {
  display: flex;
  align-items: center;
  gap: 3px;
  min-width: 0;
}
.journal-area {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 3px 7px;
  border-radius: 6px;
  font-size: 11px;
  color: var(--app-muted);
  background: color-mix(in srgb, var(--area-color) 12%, var(--app-card));
}
.journal-area > span {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background: var(--area-color);
  flex-shrink: 0;
}
.journal-menu {
  position: relative;
  flex-shrink: 0;
}
.journal-menu summary {
  list-style: none;
  display: grid;
  place-items: center;
  width: 34px;
  height: 30px;
  border-radius: 7px;
  color: var(--app-muted);
  cursor: pointer;
}
.journal-menu summary::-webkit-details-marker {
  display: none;
}
.journal-menu-items {
  position: absolute;
  right: 0;
  top: 100%;
  z-index: 6;
  display: flex;
  flex-direction: column;
  min-width: 180px;
  padding: 4px;
  background: var(--app-card);
  border: 1px solid var(--app-line);
  border-radius: 10px;
  box-shadow: 0 6px 24px #0002;
}
.journal-menu-items button {
  display: flex;
  align-items: center;
  gap: 10px;
  border: 0;
  border-radius: 6px;
  padding: 9px;
  background: transparent;
  color: var(--app-text);
  text-align: left;
  font-size: 12px;
  min-height: 36px;
}
.journal-menu-items button:hover {
  background: var(--app-soft);
}
.journal-menu-items button.danger {
  color: var(--app-alert);
}
.journal-card__text {
  font-size: 13px;
  line-height: 1.65;
  overflow-wrap: anywhere;
  color: var(--app-text);
}
.journal-card__text.folded {
  max-height: 6.6em;
  overflow: hidden;
}
.journal-card__text-inner {
  line-height: 1.65;
}
.journal-card__text-inner :deep(p) {
  margin: 0 0 0.5em;
}
.journal-card__text-inner :deep(p:last-child) {
  margin-bottom: 0;
}
.journal-card__text-inner :deep(ul),
.journal-card__text-inner :deep(ol) {
  padding-left: 20px;
  margin: 6px 0;
}
.journal-card__text-inner :deep(pre) {
  overflow: auto;
  padding: 10px;
  background: var(--app-side);
  border-radius: 6px;
}
.journal-card__text-inner :deep(code) {
  background: var(--app-side);
  font-size: 0.92em;
}
.journal-card__text-inner :deep(blockquote) {
  border-left: 3px solid var(--app-line);
  padding-left: 12px;
  margin: 8px 0;
  color: var(--app-muted);
}
.journal-card__text-inner :deep(h1),
.journal-card__text-inner :deep(h2),
.journal-card__text-inner :deep(h3) {
  font-size: 15px;
  margin: 8px 0;
  font-weight: 600;
}
.journal-card__text-inner :deep(img) {
  max-width: 100%;
}
.journal-card__text-inner :deep(table) {
  display: block;
  overflow: auto;
}
.journal-fold {
  align-self: flex-start;
  padding: 0;
  min-height: 30px;
  font-size: 11px;
  margin-top: 3px;
}
.journal-card__footer {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 6px 14px;
  font-size: 11px;
  color: var(--app-muted);
  margin-top: 10px;
}
.journal-card__footer > a,
.journal-card__footer > span {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  min-width: 0;
  overflow-wrap: anywhere;
  color: inherit;
  text-decoration: none;
}
.journal-card__footer i {
  font-size: 13px;
  flex-shrink: 0;
}
.journal-card__confirm {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 8px;
  font-size: 12px;
  color: var(--app-alert);
  margin-bottom: 8px;
}
.journal-card__confirm > div {
  display: flex;
  gap: 6px;
}
@media (max-width: 760px) {
  .journal-menu summary {
    width: 38px;
    height: 36px;
  }
  .journal-menu-items button {
    min-height: 44px;
  }
  .journal-fold {
    min-height: 36px;
  }
}
</style>
