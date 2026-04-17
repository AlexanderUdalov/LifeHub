import { defineStore } from 'pinia'
import { ref } from 'vue'

const STORAGE_KEY = 'lifehub-show-nsfw-content'

function readStored(): boolean {
  try {
    return localStorage.getItem(STORAGE_KEY) === 'true'
  } catch {
    return false
  }
}

export const useNsfwContentStore = defineStore('nsfwContent', () => {
  const showNsfwContent = ref(readStored())

  function setShowNsfwContent(value: boolean) {
    showNsfwContent.value = value
    try {
      localStorage.setItem(STORAGE_KEY, value ? 'true' : 'false')
    } catch {
      /* ignore */
    }
  }

  function addictionVisible(addiction: { isNsfw?: boolean | null }) {
    const flagged = !!addiction.isNsfw
    return showNsfwContent.value || !flagged
  }

  return { showNsfwContent, setShowNsfwContent, addictionVisible }
})
