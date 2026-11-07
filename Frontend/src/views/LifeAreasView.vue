<script setup lang="ts">
defineOptions({ name: 'LifeAreasView' })
import Card from 'primevue/card'
import EmptyState from '@/components/EmptyState.vue'
import Button from 'primevue/button'
import Skeleton from 'primevue/skeleton'
import { computed, onMounted } from 'vue'
import type { LifeAreaDTO } from '@/api/LifeAreasAPI'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { useI18n } from 'vue-i18n'
import { getLogoColorIndex } from '@/constants/logoColors'

const emit = defineEmits<{
  (e: 'edit-lifearea', area: LifeAreaDTO | null): void
}>()

const { t } = useI18n()
const lifeAreasStore = useLifeAreasStore()

onMounted(async () => {
  if (lifeAreasStore.lifeAreas.length === 0) {
    await lifeAreasStore.fetchLifeAreas()
  }
})

const sortedAreas = computed(() =>
  [...lifeAreasStore.lifeAreas].sort((a, b) => {
    const orderA = getLogoColorIndex(a.color)
    const orderB = getLogoColorIndex(b.color)
    if (orderA !== orderB) return orderA - orderB
    return a.name.localeCompare(b.name)
  })
)

const segmentSize = computed(() =>
  sortedAreas.value.length ? 360 / sortedAreas.value.length : 0
)

const wheelStyle = computed(() => {
  if (!sortedAreas.value.length) {
    return { background: 'var(--p-content-border-color)' }
  }

  const size = segmentSize.value
  const segments = sortedAreas.value.map((area, index) => {
    const from = index * size
    const to = (index + 1) * size
    return `${area.color} ${from}deg ${to}deg`
  })

  return {
    background: `conic-gradient(${segments.join(', ')})`
  }
})

function getSectorCenterAngle(index: number): number {
  return (index + 0.5) * segmentSize.value
}

/** Текст на сегменте круга: иконка или первая буква названия. */
function getSegmentLabel(area: LifeAreaDTO): string {
  if (area.emoji?.trim()) return area.emoji.trim()
  const name = area.name.trim()
  return name ? name.charAt(0).toUpperCase() : '?'
}

function onEditArea(area: LifeAreaDTO) {
  emit('edit-lifearea', area)
}

</script>

<template>
  <div class="lifeareas-view">
    <header class="lifeareas-view__header">
      <h1 class="ds-page-header">{{ $t('lifeareas.title') }}</h1>
      <Button v-if="!lifeAreasStore.isLimitReached" :label="$t('lifeareas.add')" icon="pi pi-plus"
        class="desktop-create-btn" @click="emit('edit-lifearea', null)" />
    </header>

    <div v-if="lifeAreasStore.isLoading && lifeAreasStore.lifeAreas.length === 0" class="lifeareas-skeleton">
      <Skeleton shape="circle" size="170px" class="skeleton-wheel" />
      <div class="skeleton-legend">
        <Skeleton v-for="i in 3" :key="i" height="3rem" class="skeleton-legend-card" />
      </div>
    </div>

    <EmptyState v-else-if="sortedAreas.length === 0" icon="pi pi-chart-pie"
      :title="$t('lifeareas.empty')" :subtitle="$t('lifeareas.emptySubtitle')" />

    <template v-else>
      <div class="wheel-wrap">
      <div class="wheel" :style="wheelStyle" aria-hidden="true" />
      <div v-if="sortedAreas.length" class="wheel-emojis" aria-hidden="true">
        <span v-for="(area, index) in sortedAreas" :key="area.id" class="wheel-emoji" :class="{ 'wheel-emoji--text': !area.emoji?.trim() }" :style="{
          transform: `rotate(${getSectorCenterAngle(index)}deg) translate(0, -200%) rotate(${-getSectorCenterAngle(index)}deg)`
        }">{{ getSegmentLabel(area) }}</span>
      </div>
    </div>

    <div class="legend">
      <Card v-for="area in sortedAreas" :key="area.id" class="legend-card" :style="{ borderLeftColor: area.color }"
        @click="onEditArea(area)">
        <template #title>
          <span v-if="area.emoji?.trim()" class="legend-icon">{{ area.emoji }}</span>
          <span v-else class="legend-initial">{{ getSegmentLabel(area) }}</span>
          {{ area.name }}
        </template>
      </Card>
    </div>
    </template>
  </div>
</template>

<style scoped>
.lifeareas-view {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  align-items: center;
  padding: 0 1rem 2rem;
}

.lifeareas-skeleton {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  align-items: center;
}

.lifeareas-view__header {
  display: contents;
}

.desktop-create-btn {
  display: none;
}

.skeleton-wheel {
  width: min(72vw, 170px);
  max-width: 170px;
  aspect-ratio: 1 / 1;
  flex-shrink: 0;
}

.skeleton-legend {
  width: 100%;
  max-width: 400px;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.skeleton-legend-card {
  border-radius: var(--p-border-radius);
}

.wheel-wrap {
  position: relative;
  width: min(72vw, 170px);
  max-width: 170px;
  aspect-ratio: 1 / 1;
  flex-shrink: 0;
  margin: 0 auto;
}

.wheel {
  width: 100%;
  height: 100%;
  border-radius: 9999px;
  border: 2px solid var(--p-content-border-color);
  pointer-events: none;
}

.wheel-emojis {
  position: absolute;
  inset: 0;
  border-radius: 9999px;
  pointer-events: none;
}

.wheel-emoji {
  position: absolute;
  left: 50%;
  top: 50%;
  width: 1.5em;
  height: 1.5em;
  margin-left: -0.75em;
  margin-top: -0.75em;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 9999px;
  font-size: 1.25rem;
  line-height: 1;
  z-index: 0;
}

.wheel-emoji::before {
  content: '';
  position: absolute;
  inset: -0.2em;
  border-radius: inherit;
  background: radial-gradient(circle at 30% 30%,
      rgba(255, 255, 255, 0.45) 0%,
      rgba(255, 255, 255, 0.18) 45%,
      rgba(0, 0, 0, 0.3) 100%);
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.24);
  z-index: -1;
}

.wheel-emoji--text {
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--p-text-color);
  text-shadow: 0 0 1px var(--p-content-background), 0 1px 2px rgba(0, 0, 0, 0.15);
}

.legend {
  width: 100%;
  max-width: 400px;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.legend-card {
  width: 100%;
  border-left-width: 4px;
  border-left-style: solid;
  cursor: pointer;
  transition: background-color 0.15s, border-color 0.15s;
}

.legend-card :deep(.p-card-body) {
  padding-top: 0.5rem;
  padding-bottom: 0.5rem;
  flex-direction: row;
}

.legend-card :deep(.p-card-title) {
  font-size: 0.9rem;
}

.legend-icon,
.legend-initial {
  margin-right: 0.35em;
}

.legend-initial {
  font-weight: 700;
  opacity: 0.9;
}

.legend-card:hover {
  background: var(--p-content-hover-background);
  border-color: var(--p-content-hover-border-color);
}

@media (min-width: 900px) {
  .lifeareas-view {
    display: grid;
    grid-template-columns: minmax(18rem, 24rem) minmax(20rem, 1fr);
    align-items: start;
    gap: 2rem;
    max-width: 72rem;
    margin: 0 auto;
    padding: 0;
  }

  .lifeareas-view__header,
  .lifeareas-skeleton,
  .lifeareas-view :deep(.empty-state) {
    grid-column: 1 / -1;
  }

  .lifeareas-view__header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    width: 100%;
  }

  .desktop-create-btn {
    display: inline-flex;
  }

  .lifeareas-view :deep(.empty-state) {
    justify-self: center;
  }

  .lifeareas-skeleton {
    display: grid;
    grid-template-columns: minmax(18rem, 24rem) minmax(20rem, 1fr);
    align-items: start;
    width: 100%;
  }

  .wheel-wrap,
  .skeleton-wheel {
    width: min(28vw, 22rem);
    max-width: 22rem;
  }

  .legend,
  .skeleton-legend {
    max-width: none;
  }
}
</style>
