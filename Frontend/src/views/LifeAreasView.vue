<script setup lang="ts">
import Card from 'primevue/card'
import { computed, onMounted } from 'vue'
import type { LifeAreaDTO } from '@/api/LifeAreasAPI'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { useI18n } from 'vue-i18n'

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
  [...lifeAreasStore.lifeAreas].sort((a, b) => a.name.localeCompare(b.name))
)

const wheelStyle = computed(() => {
  if (!sortedAreas.value.length) {
    return { background: 'var(--p-content-border-color)' }
  }

  const segmentSize = 360 / sortedAreas.value.length
  const segments = sortedAreas.value.map((area, index) => {
    const from = index * segmentSize
    const to = (index + 1) * segmentSize
    return `${area.color} ${from}deg ${to}deg`
  })

  return {
    background: `conic-gradient(${segments.join(', ')})`
  }
})

function onEditArea(area: LifeAreaDTO) {
  emit('edit-lifearea', area)
}

</script>

<template>
  <div class="lifeareas-view">
    <h1 class="view-header">{{ t('lifeareas.title') }}</h1>

    <div class="wheel-wrap">
      <div class="wheel" :style="wheelStyle" aria-hidden="true" />
    </div>

    <div class="legend">
      <Card v-for="area in sortedAreas" :key="area.id" class="legend-card" :style="{ borderLeftColor: area.color }"
        @click="onEditArea(area)">
        <template #title>{{ area.name }}</template>
      </Card>
    </div>
  </div>
</template>

<style scoped>
.lifeareas-view {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  align-items: center;
  padding: 1rem;
  padding-bottom: 2rem;
}

.view-header {
  margin: 0;
  font-size: 1.5rem;
  font-weight: 600;
  color: var(--p-text-color);
  width: 100%;
  text-align: center;
}

.wheel-wrap {
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
}

.legend-card:hover {
  background: var(--p-content-hover-background);
  border-color: var(--p-content-hover-border-color);
}
</style>
