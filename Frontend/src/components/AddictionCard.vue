<script setup lang="ts">
import Card from 'primevue/card'
import Button from 'primevue/button'
import Drawer from 'primevue/drawer'
import AddictionCalendar from './AddictionCalendar.vue'
import AddictionResetModal from './AddictionResetModal.vue'
import AddictionTriggerModal from './AddictionTriggerModal.vue'
import type { AddictionDTO, AddictionWithResetsDTO } from '@/api/AddictionsAPI'
import { computed, nextTick, onMounted, onUnmounted, ref } from 'vue'
import { parseUtcIso, toDateOnlyString } from '@/utils/dateOnly'
import { useI18n } from 'vue-i18n'
import { useAddictionsStore } from '@/stores/addictions'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { useGoalsStore } from '@/stores/goals'
import { useAddictionProgress } from '@/composables/useAddictionProgress'

const props = withDefaults(
  defineProps<{ addiction: AddictionWithResetsDTO; noBorder?: boolean }>(),
  { noBorder: false }
)

const emit = defineEmits<{
  (e: 'edit', addiction: AddictionDTO): void
}>()

const { t, locale } = useI18n()
const addictionsStore = useAddictionsStore()
const lifeAreasStore = useLifeAreasStore()
const goalsStore = useGoalsStore()

const intlLocale = computed(() => (locale.value === 'ru' ? 'ru-RU' : 'en-US'))

const areaColor = computed(() => lifeAreasStore.getAreaColorById(props.addiction.addiction.lifeAreaId))

const displayColor = computed(() => {
  const fromArea = areaColor.value
  if (fromArea?.trim()) return fromArea.trim()
  const c = props.addiction.addiction.color?.trim()
  return c && c.length > 0 ? c : '#ef4444'
})

const cardBorderStyle = computed(() => {
  if (props.noBorder) return { border: 'none', borderLeftWidth: 0 }
  return {
    borderLeftWidth: '4px',
    borderLeftStyle: 'solid',
    borderLeftColor: displayColor.value
  }
})

const goalTitle = computed(() => goalsStore.getGoalById(props.addiction.addiction.goalId)?.title ?? null)

const resetsList = computed(() => props.addiction.resets ?? [])

// --- Timer (ticks every second) ---
const now = ref(new Date())
let tickInterval: ReturnType<typeof setInterval> | undefined
onMounted(() => {
  tickInterval = setInterval(() => {
    now.value = new Date()
  }, 1_000)
})
onUnmounted(() => {
  if (tickInterval) clearInterval(tickInterval)
})

const addictionRef = computed(() => props.addiction)
const { elapsed, nextMilestone, progressPercent } = useAddictionProgress(addictionRef, now)

const progressText = computed(() => {
  const e = elapsed.value
  const parts: string[] = []
  if (e.months > 0) parts.push(`${e.months}${t('addictions.units.mo')}`)
  if (e.days > 0) parts.push(`${e.days}${t('addictions.units.d')}`)
  if (e.hours > 0) parts.push(`${e.hours}${t('addictions.units.h')}`)
  parts.push(`${e.minutes}${t('addictions.units.min')}`)
  parts.push(`${e.seconds}${t('addictions.units.s')}`)
  return parts.join(' ')
})

const milestoneLabel = computed(() => {
  const ms = nextMilestone.value
  if (!ms) return t('addictions.maxStage')
  return t('addictions.nextMilestone', { milestone: t(ms.labelKey) })
})

const isExpanded = ref(false)

function toggleExpanded() {
  isExpanded.value = !isExpanded.value
}

const showResetDrawer = ref(false)
const resetPrefilledDate = ref<Date | null>(null)
const showTriggerDrawer = ref(false)
const showResetInfoDrawer = ref(false)
const resetInfoDate = ref<Date | null>(null)

function openResetDrawer(date?: Date) {
  resetPrefilledDate.value = date ?? null
  showResetDrawer.value = true
}

function openTriggerDrawer() {
  showTriggerDrawer.value = true
}

async function onResetConfirm(payload: { addiction: AddictionDTO; resetAt: Date; note: string }) {
  showResetDrawer.value = false
  await addictionsStore.setReset(payload.addiction.id, payload.resetAt, {
    note: payload.note,
    resetAt: payload.resetAt
  })
}

function onCalendarCellClick(date: Date, hasReset: boolean) {
  if (hasReset) {
    resetInfoDate.value = date
    showResetInfoDrawer.value = true
  } else {
    openResetDrawer(date)
  }
}

async function onAddAnotherResetForDay() {
  if (!resetInfoDate.value) return
  const d = new Date(
    resetInfoDate.value.getFullYear(),
    resetInfoDate.value.getMonth(),
    resetInfoDate.value.getDate()
  )
  showResetInfoDrawer.value = false
  await nextTick()
  openResetDrawer(d)
}

const resetInfoDateLabel = computed(() => {
  if (!resetInfoDate.value) return ''
  return resetInfoDate.value.toLocaleDateString(intlLocale.value, {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric'
  })
})

const resetsForInfoDay = computed(() => {
  if (!resetInfoDate.value) return []
  const key = toDateOnlyString(resetInfoDate.value)
  return [...resetsList.value]
    .filter((r) => r.date === key)
    .sort((a, b) => a.resetAt.localeCompare(b.resetAt))
})

function formatResetAt(iso: string) {
  return parseUtcIso(iso).toLocaleString(intlLocale.value, {
    dateStyle: 'medium',
    timeStyle: 'short'
  })
}
</script>

<template>
  <Card class="addiction-card" :class="{ 'no-border': noBorder }" :style="cardBorderStyle">
    <template #title>
      <div class="ac-header">
        <div class="ac-title-wrap">
          <div class="ac-title" @click="emit('edit', addiction.addiction)">
            {{ addiction.addiction.title }}
          </div>
          <div v-if="goalTitle && !isExpanded" class="ac-goal">{{ goalTitle }}</div>
        </div>
        <Button
          :icon="isExpanded ? 'pi pi-chevron-up' : 'pi pi-calendar'"
          text
          rounded
          size="small"
          class="ac-toggle-btn"
          @click="toggleExpanded"
        />
      </div>
    </template>

    <template #content>
      <div v-if="!isExpanded" class="ac-compact">
        <div class="ac-timer">{{ progressText }}</div>

        <div class="ac-progress-row">
          <div class="ac-progress-track">
            <div
              class="ac-progress-fill"
              :style="{
                width: progressPercent + '%',
                backgroundColor: displayColor
              }"
            />
          </div>
          <span class="ac-milestone">{{ milestoneLabel }}</span>
        </div>

        <div class="ac-actions">
          <Button
            :label="t('addictions.trigger')"
            icon="pi pi-bolt"
            severity="warn"
            outlined
            size="small"
            @click="openTriggerDrawer"
          />
          <Button
            :label="t('addictions.reset')"
            icon="pi pi-undo"
            severity="danger"
            size="small"
            @click="openResetDrawer()"
          />
        </div>
      </div>

      <div v-else class="ac-expanded">
        <AddictionCalendar
          :resets="resetsList"
          :created-at="addiction.addiction.createdAt"
          :color="displayColor"
          @cell-click="onCalendarCellClick"
        />
      </div>
    </template>
  </Card>

  <AddictionResetModal
    v-if="showResetDrawer"
    :addiction="addiction.addiction"
    :prefilled-date="resetPrefilledDate"
    @confirm="onResetConfirm"
    @close="showResetDrawer = false"
  />

  <AddictionTriggerModal
    v-if="showTriggerDrawer"
    :addiction-title="addiction.addiction.title"
    @close="showTriggerDrawer = false"
  />

  <Drawer
    v-model:visible="showResetInfoDrawer"
    position="bottom"
    class="addiction-drawer"
    style="height: auto; max-height: 85vh"
  >
    <template #header>
      <div class="reset-info-drawer-title">
        {{ t('addictions.resetInfo.title', { date: resetInfoDateLabel }) }}
      </div>
    </template>

    <div v-if="resetsForInfoDay.length === 0" class="reset-info-empty">
      {{ t('addictions.resetInfo.empty') }}
    </div>
    <div v-else class="reset-info-cards">
      <div v-for="r in resetsForInfoDay" :key="r.id" class="reset-info-card">
        <div class="reset-info-card-time">{{ formatResetAt(r.resetAt) }}</div>
        <div class="reset-info-card-note">
          {{ r.journalText?.trim() ? r.journalText.trim() : t('addictions.resetInfo.noNote') }}
        </div>
      </div>
    </div>
  </Drawer>
</template>

<style scoped>
.addiction-card {
  border-radius: 16px;
  border-left-width: 4px;
  border-left-style: solid;
}

.addiction-card.no-border {
  border: none;
  border-left-width: 0;
}

.ac-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 8px;
  width: 100%;
}

.ac-title-wrap {
  min-width: 0;
  flex: 1;
}

.ac-title {
  cursor: pointer;
  user-select: none;
  font-weight: 600;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.ac-goal {
  font-size: 0.8rem;
  color: var(--p-text-muted-color);
  margin-top: 2px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.ac-toggle-btn {
  flex-shrink: 0;
}

.ac-compact {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.ac-timer {
  font-size: 1rem;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
  letter-spacing: 0.02em;
  color: var(--p-text-color);
}

.ac-progress-row {
  display: flex;
  align-items: center;
  gap: 10px;
}

.ac-progress-track {
  flex: 1;
  height: 6px;
  border-radius: 3px;
  background-color: var(--p-content-border-color);
  overflow: hidden;
}

.ac-progress-fill {
  height: 100%;
  border-radius: 3px;
  transition: width 1s linear;
}

.ac-milestone {
  font-size: 0.75rem;
  color: var(--p-text-muted-color);
  white-space: nowrap;
  flex-shrink: 0;
}

.ac-actions {
  display: flex;
  justify-content: space-between;
  gap: 8px;
}

.ac-expanded {
  padding-top: 4px;
}

.reset-info-drawer-title {
  font-weight: 600;
  font-size: 1rem;
  line-height: 1.3;
  padding-right: 0.5rem;
}

.reset-info-empty {
  color: var(--p-text-muted-color);
  font-size: 0.9rem;
  padding: 8px 0;
}

.reset-info-cards {
  display: flex;
  flex-direction: column;
  gap: 10px;
  max-height: 55vh;
  overflow-y: auto;
}

.reset-info-card {
  border: 1px solid var(--p-content-border-color);
  border-radius: 12px;
  padding: 12px 14px;
  background: var(--p-content-background);
}

.reset-info-card-time {
  font-weight: 600;
  font-size: 0.9rem;
  margin-bottom: 6px;
}

.reset-info-card-note {
  font-size: 0.875rem;
  color: var(--p-text-color);
  line-height: 1.45;
  white-space: pre-wrap;
}

.reset-info-drawer-footer {
  display: flex;
  justify-content: stretch;
  width: 100%;
}

.reset-info-add-btn {
  width: 100%;
}
</style>
