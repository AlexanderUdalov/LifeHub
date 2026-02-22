import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import {
  addictionsApi,
  type AddictionUpsertRequest,
  type AddictionWithResetsDTO
} from '@/api/AddictionsAPI'
import { toDateOnlyString } from '@/utils/dateOnly'

export const useAddictionsStore = defineStore('addictions', () => {
  const addictions = ref<AddictionWithResetsDTO[]>([])
  const isLoading = ref(false)
  const rangeDays = ref(60)

  async function fetchAddictions(days = rangeDays.value) {
    rangeDays.value = days
    isLoading.value = true
    try {
      addictions.value = await addictionsApi.getAddictions(days)
    } finally {
      isLoading.value = false
    }
  }

  const addictionsSorted = computed(() =>
    [...addictions.value].sort((a, b) => a.addiction.title.localeCompare(b.addiction.title))
  )

  async function setReset(addictionId: string, date: Date) {
    const a = addictions.value.find((x) => x.addiction.id === addictionId)
    if (!a) return

    const dateKey = toDateOnlyString(date)
    const prev = [...a.resetDates]
    a.resetDates.push(dateKey)
    a.resetDates.sort()

    try {
      await addictionsApi.setReset(addictionId, date)
      if (toDateOnlyString(date) === toDateOnlyString(new Date())) {
        a.lastResetAt = new Date().toISOString()
      }
    } catch (e) {
      a.resetDates = prev
      throw e
    }
  }

  async function removeReset(addictionId: string, date: Date) {
    const a = addictions.value.find((x) => x.addiction.id === addictionId)
    if (!a) return

    const dateKey = toDateOnlyString(date)
    const prev = [...a.resetDates]
    const idx = a.resetDates.lastIndexOf(dateKey)
    if (idx === -1) return

    a.resetDates.splice(idx, 1)

    try {
      await addictionsApi.removeReset(addictionId, date)
      if (toDateOnlyString(date) === toDateOnlyString(new Date())) {
        a.lastResetAt = null
      }
    } catch (e) {
      a.resetDates = prev
      throw e
    }
  }

  async function createAddiction(request: AddictionUpsertRequest) {
    const created = await addictionsApi.createAddiction(request)
    addictions.value.push({ addiction: created, resetDates: [], lastResetAt: null })
  }

  async function updateAddiction(id: string, request: AddictionUpsertRequest) {
    const existing = addictions.value.find((x) => x.addiction.id === id)
    if (!existing) return

    const backup = { ...existing.addiction }
    Object.assign(existing.addiction, request)

    try {
      const updated = await addictionsApi.updateAddiction(id, request)
      existing.addiction = updated
    } catch (e) {
      existing.addiction = backup
      throw e
    }
  }

  async function deleteAddiction(id: string) {
    const idx = addictions.value.findIndex((x) => x.addiction.id === id)
    if (idx === -1) return

    const backup = addictions.value[idx]
    addictions.value.splice(idx, 1)

    try {
      await addictionsApi.deleteAddiction(id)
    } catch (e) {
      if (backup) addictions.value.splice(idx, 0, backup)
      throw e
    }
  }

  return {
    addictions,
    addictionsSorted,
    isLoading,
    rangeDays,
    fetchAddictions,
    setReset,
    removeReset,
    createAddiction,
    updateAddiction,
    deleteAddiction
  }
})
