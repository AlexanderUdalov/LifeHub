import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import {
  addictionsApi,
  type AddictionTriggerOutcome,
  type AddictionUpsertRequest,
  type AddictionWithResetsDTO
} from '@/api/AddictionsAPI'

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

  /** Expands loaded history window when the UI needs deeper stats (API max 365 days). */
  async function ensureMinHistoryDays(minDays: number) {
    const capped = Math.min(365, Math.max(1, minDays))
    if (rangeDays.value < capped) await fetchAddictions(capped)
  }

  const addictionsSorted = computed(() =>
    [...addictions.value].sort((a, b) => a.addiction.title.localeCompare(b.addiction.title))
  )

  async function setReset(
    addictionId: string,
    date: Date,
    options?: { note?: string; resetAt?: Date }
  ) {
    try {
      await addictionsApi.setReset(addictionId, date, {
        note: options?.note ?? null,
        resetAt: options?.resetAt ?? null
      })
      await fetchAddictions(rangeDays.value)
    } catch (e) {
      throw e
    }
  }

  async function removeReset(addictionId: string, date: Date) {
    try {
      await addictionsApi.removeReset(addictionId, date)
      await fetchAddictions(rangeDays.value)
    } catch (e) {
      throw e
    }
  }

  async function createAddiction(request: AddictionUpsertRequest) {
    await addictionsApi.createAddiction(request)
    await fetchAddictions(rangeDays.value)
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

  async function logTriggerEvent(
    addictionId: string,
    outcome: AddictionTriggerOutcome,
    options?: { note?: string | null; eventAt?: string | null; language?: string | null }
  ) {
    await addictionsApi.logTriggerEvent(
      addictionId,
      {
        outcome,
        note: options?.note ?? null,
        eventAt: options?.eventAt ?? null
      },
      options?.language ?? null
    )
    await fetchAddictions(rangeDays.value)
  }

  return {
    addictions,
    addictionsSorted,
    isLoading,
    rangeDays,
    fetchAddictions,
    ensureMinHistoryDays,
    setReset,
    removeReset,
    logTriggerEvent,
    createAddiction,
    updateAddiction,
    deleteAddiction
  }
})
