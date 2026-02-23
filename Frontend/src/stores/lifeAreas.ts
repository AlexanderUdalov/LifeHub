import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import {
  lifeAreasApi,
  type CreateLifeAreaRequest,
  type LifeAreaDTO,
  type UpdateLifeAreaRequest
} from '@/api/LifeAreasAPI'

const LIFE_AREA_LIMIT = 10

export const useLifeAreasStore = defineStore('lifeAreas', () => {
  const lifeAreas = ref<LifeAreaDTO[]>([])
  const isLoading = ref(false)

  const isLimitReached = computed(() => lifeAreas.value.length >= LIFE_AREA_LIMIT)

  function getAreaColorById(lifeAreaId: string | null | undefined): string | null {
    if (!lifeAreaId) return null
    return lifeAreas.value.find((x) => x.id === lifeAreaId)?.color ?? null
  }

  async function fetchLifeAreas() {
    isLoading.value = true
    try {
      lifeAreas.value = await lifeAreasApi.getLifeAreas()
    } finally {
      isLoading.value = false
    }
  }

  async function createLifeArea(request: CreateLifeAreaRequest) {
    if (isLimitReached.value) {
      throw new Error('LIFE_AREA_LIMIT_REACHED')
    }

    const created = await lifeAreasApi.createLifeArea(request)
    lifeAreas.value.push(created)
  }

  async function updateLifeArea(id: string, request: UpdateLifeAreaRequest) {
    const existing = lifeAreas.value.find((x) => x.id === id)
    if (!existing) return

    const backup = { ...existing }
    Object.assign(existing, request)

    try {
      const updated = await lifeAreasApi.updateLifeArea(id, request)
      Object.assign(existing, updated)
    } catch (e) {
      Object.assign(existing, backup)
      throw e
    }
  }

  async function deleteLifeArea(id: string) {
    const idx = lifeAreas.value.findIndex((x) => x.id === id)
    if (idx === -1) return

    const backup = lifeAreas.value[idx]
    lifeAreas.value.splice(idx, 1)

    try {
      await lifeAreasApi.deleteLifeArea(id)
    } catch (e) {
      if (backup) lifeAreas.value.splice(idx, 0, backup)
      throw e
    }
  }

  return {
    lifeAreas,
    isLoading,
    isLimitReached,
    fetchLifeAreas,
    createLifeArea,
    updateLifeArea,
    deleteLifeArea,
    getAreaColorById
  }
})
