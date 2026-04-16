<script setup lang="ts">
import Button from 'primevue/button'
import ProgressSpinner from 'primevue/progressspinner'
import { computed, onMounted, onUnmounted, ref, shallowRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import BaseDrawer from '@/components/base/BaseDrawer.vue'
import type { AddictionWithResetsDTO } from '@/api/AddictionsAPI'
import { useAddictionsStore } from '@/stores/addictions'
import { useAddictionProgress } from '@/composables/useAddictionProgress'
import { buildAddictionStats } from '@/utils/addictionStats'
import { fromDateOnlyString } from '@/utils/dateOnly'

const props = defineProps<{
  addictionId: string
  visible: boolean
  accentColor?: string
  initialAddiction?: AddictionWithResetsDTO
}>()

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void
}>()

const { t, locale } = useI18n()
const addictionsStore = useAddictionsStore()

const intlLocale = computed(() => (locale.value === 'ru' ? 'ru-RU' : 'en-US'))

const model = shallowRef<AddictionWithResetsDTO | null>(null)
const isLoading = ref(false)
const loadError = ref('')

const innerVisible = computed({
  get: () => props.visible,
  set: (v: boolean) => emit('update:visible', v)
})

async function loadModel() {
  loadError.value = ''
  isLoading.value = true
  try {
    model.value = props.initialAddiction ?? null
    await addictionsStore.ensureMinHistoryDays(365)
    const found = addictionsStore.addictions.find((a) => a.addiction.id === props.addictionId) ?? null
    model.value = found ?? props.initialAddiction ?? null
    if (!model.value) loadError.value = t('addictions.stats.notFound')
  } finally {
    isLoading.value = false
  }
}

watch(
  () => [props.visible, props.addictionId] as const,
  ([vis]) => {
    if (vis) void loadModel()
    else {
      model.value = null
      loadError.value = ''
    }
  },
  { immediate: true }
)

const now = ref(new Date())
let tick: ReturnType<typeof setInterval> | undefined
onMounted(() => {
  tick = setInterval(() => {
    now.value = new Date()
  }, 1_000)
})
onUnmounted(() => {
  if (tick) clearInterval(tick)
})

const progressSource = computed(() => model.value ?? props.initialAddiction ?? null)

const progressModel = computed(() => {
  const m = progressSource.value
  if (!m) {
    return {
      addiction: {
        id: '',
        title: '',
        description: null,
        color: '#888',
        createdAt: now.value.toISOString(),
        goalId: null,
        lifeAreaId: null
      },
      resets: [],
      triggerEvents: [],
      lastResetAt: null,
      currentStreakDays: 0
    } as AddictionWithResetsDTO
  }
  return m
})

const { elapsed, nextMilestone, progressPercent } = useAddictionProgress(progressModel, now)

const progressText = computed(() => {
  if (!progressSource.value) return '—'
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

const currentStreakDaysLabel = computed(() => {
  const m = progressSource.value
  if (!m) return '—'
  return t('addictions.stats.currentStreakDays', { count: m.currentStreakDays })
})

const stats = computed(() => {
  const m = model.value
  if (!m) return null
  return buildAddictionStats(m, now.value, 8, intlLocale.value)
})

const avgStreakLabel = computed(() => {
  const s = stats.value
  if (!s) return '—'
  const n = s.avgStreakDays
  if (Number.isInteger(n)) return t('addictions.daysCount', { count: n })
  return `${n} ${t('addictions.units.d')}`
})

const weekTrendMax = computed(() => {
  const s = stats.value
  if (!s) return 1
  return Math.max(1, ...s.weekTrend.map((w) => w.count))
})

const weekdayMax = computed(() => {
  const s = stats.value
  if (!s) return 1
  return Math.max(1, ...s.resetByWeekdayMonFirst)
})

const weekdayLabels = computed(() => {
  const base = fromDateOnlyString('2024-01-01')
  const labels: string[] = []
  for (let i = 0; i < 7; i++) {
    const d = new Date(base)
    d.setDate(base.getDate() + i)
    labels.push(d.toLocaleDateString(intlLocale.value, { weekday: 'short' }))
  }
  return labels
})

const accent = computed(() => props.accentColor?.trim() || 'var(--p-primary-color)')

function barHeightPx(count: number, max: number, maxPx: number): string {
  if (max <= 0) return '3px'
  if (count <= 0) return '3px'
  return `${Math.max(6, Math.round((count / max) * maxPx))}px`
}
</script>

<template>
  <BaseDrawer v-model:visible="innerVisible" class="addiction-stats-drawer" max-height="90vh" min-height="50vh">
    <template #header>
      <div class="stats-header">
        <span class="stats-title">{{ t('addictions.stats.title') }}</span>
        <Button
          icon="pi pi-times"
          severity="secondary"
          text
          rounded
          :aria-label="t('addictions.stats.close')"
          @click="innerVisible = false"
        />
      </div>
    </template>

    <div class="stats-body">
      <div v-if="isLoading" class="stats-loading">
        <ProgressSpinner stroke-width="4" style="width: 2.5rem; height: 2.5rem" />
      </div>
      <div v-else-if="loadError" class="stats-error">{{ loadError }}</div>
      <template v-else-if="model">
        <section class="stats-section">
          <h3 class="stats-h">{{ t('addictions.stats.sectionStreak') }}</h3>
          <div class="stats-grid">
            <div class="stat-cell">
              <div class="stat-label">{{ t('addictions.stats.sinceLastReset') }}</div>
              <div class="stat-value">{{ progressText }}</div>
            </div>
            <div class="stat-cell">
              <div class="stat-label">{{ t('addictions.stats.calendarStreak') }}</div>
              <div class="stat-value">{{ currentStreakDaysLabel }}</div>
            </div>
            <div class="stat-cell wide">
              <div class="stat-label">{{ t('addictions.stats.nextMilestoneHeading') }}</div>
              <div class="stat-milestone-row">
                <div class="mini-track">
                  <div
                    class="mini-fill"
                    :style="{ width: progressPercent + '%', backgroundColor: accent }"
                  />
                </div>
                <span class="stat-sub">{{ milestoneLabel }}</span>
              </div>
            </div>
            <div class="stat-cell">
              <div class="stat-label">{{ t('addictions.stats.maxStreak') }}</div>
              <div class="stat-value">
                {{ stats ? t('addictions.daysCount', { count: stats.maxStreakDays }) : '—' }}
              </div>
            </div>
            <div class="stat-cell">
              <div class="stat-label">{{ t('addictions.stats.avgStreak') }}</div>
              <div class="stat-value">{{ avgStreakLabel }}</div>
            </div>
          </div>
        </section>

        <section class="stats-section">
          <h3 class="stats-h">{{ t('addictions.stats.sectionResets') }}</h3>
          <div class="stats-grid">
            <div class="stat-cell">
              <div class="stat-label">{{ t('addictions.stats.resetCount') }}</div>
              <div class="stat-value">{{ stats?.resetCount ?? 0 }}</div>
            </div>
            <div class="stat-cell wide">
              <div class="stat-label">
                {{ t('addictions.stats.cleanPercent30', { days: stats?.daysInLast30Window ?? 30 }) }}
              </div>
              <div class="stat-value accent">{{ stats?.cleanDaysPercentLast30 ?? 0 }}%</div>
            </div>
          </div>
        </section>

        <section class="stats-section">
          <h3 class="stats-h">{{ t('addictions.stats.sectionWeekTrend') }}</h3>
          <div class="bar-chart bar-chart-week">
            <div v-for="w in stats?.weekTrend ?? []" :key="w.weekKey" class="bar-col">
              <div
                class="bar-pill"
                :style="{
                  height: barHeightPx(w.count, weekTrendMax, 80),
                  backgroundColor: accent
                }"
              />
              <span class="bar-x">{{ w.label }}</span>
              <span class="bar-n">{{ w.count }}</span>
            </div>
          </div>
        </section>

        <section class="stats-section">
          <h3 class="stats-h">{{ t('addictions.stats.sectionWeekday') }}</h3>
          <div class="bar-chart bar-chart-dow">
            <div
              v-for="(c, i) in stats?.resetByWeekdayMonFirst ?? []"
              :key="i"
              class="bar-col"
            >
              <div
                class="bar-pill"
                :style="{
                  height: barHeightPx(c, weekdayMax, 80),
                  backgroundColor: accent
                }"
              />
              <span class="bar-x">{{ weekdayLabels[i] }}</span>
              <span class="bar-n">{{ c }}</span>
            </div>
          </div>
        </section>

        <section class="stats-section">
          <h3 class="stats-h">{{ t('addictions.stats.sectionTriggers') }}</h3>
          <div class="stats-grid">
            <div class="stat-cell">
              <div class="stat-label">{{ t('addictions.stats.triggerCount') }}</div>
              <div class="stat-value">{{ stats?.triggerTotal ?? 0 }}</div>
            </div>
            <div class="stat-cell">
              <div class="stat-label">{{ t('addictions.stats.triggerSuccess') }}</div>
              <div class="stat-value">
                {{
                  stats?.triggerSuccessPercent == null ? '—' : `${stats.triggerSuccessPercent}%`
                }}
              </div>
            </div>
          </div>
        </section>
      </template>
    </div>
  </BaseDrawer>
</template>

<style scoped>
.addiction-stats-drawer {
  --stats-accent: v-bind(accent);
}

.stats-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--ds-space-2);
  width: 100%;
  padding-right: 0.25rem;
}

.stats-title {
  font-weight: 600;
  font-size: 1.05rem;
}

.stats-body {
  padding: 0 2px 12px;
  min-height: 120px;
}

.stats-loading,
.stats-error {
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 24px;
  color: var(--p-text-muted-color);
}

.stats-error {
  color: var(--p-red-500);
}

.stats-section {
  margin-bottom: var(--ds-space-5);
}

.stats-section:last-child {
  margin-bottom: 0;
}

.stats-h {
  font-size: 0.8rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--p-text-muted-color);
  margin: 0 0 var(--ds-space-3);
}

.stats-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--ds-space-3);
}

.stat-cell {
  border: 1px solid var(--p-content-border-color);
  border-radius: var(--ds-radius-lg);
  padding: 12px 14px;
  background: var(--p-content-background);
}

.stat-cell.wide {
  grid-column: 1 / -1;
}

.stat-label {
  font-size: 0.75rem;
  color: var(--p-text-muted-color);
  margin-bottom: 6px;
}

.stat-value {
  font-size: 1.25rem;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}

.stat-value.accent {
  color: var(--stats-accent);
}

.stat-milestone-row {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.stat-sub {
  font-size: 0.85rem;
  color: var(--p-text-color);
}

.mini-track {
  height: 0.35rem;
  border-radius: var(--ds-radius-sm);
  background: var(--p-content-border-color);
  overflow: hidden;
}

.mini-fill {
  height: 100%;
  border-radius: var(--ds-radius-sm);
  transition: width 1s linear;
}

.bar-chart {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 6px;
  min-height: 120px;
  padding: 8px 4px 0;
  border: 1px solid var(--p-content-border-color);
  border-radius: var(--ds-radius-lg);
  background: var(--p-content-background);
}

.bar-col {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  min-width: 0;
}

.bar-pill {
  width: 100%;
  max-width: 28px;
  border-radius: 6px 6px 2px 2px;
  align-self: center;
  transition: height 0.25s ease;
}

.bar-chart-week .bar-col,
.bar-chart-dow .bar-col {
  min-height: 100px;
  justify-content: flex-end;
}

.bar-x {
  font-size: 0.65rem;
  color: var(--p-text-muted-color);
  text-align: center;
  line-height: 1.2;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  width: 100%;
}

.bar-n {
  font-size: 0.7rem;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  color: var(--p-text-color);
}
</style>
