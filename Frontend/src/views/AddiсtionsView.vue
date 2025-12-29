<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { addictionsApi } from '@/api/AddictionAPI'
import type { AddictionItem } from '@/models/AddictionItem'

import AddictionCard from '@/components/AddictionCard.vue'
import AddictionTriggerModal from '@/components/AddictionTriggerModal.vue'
import AddictionResetModal from '@/components/AddictionResetModal.vue'

const addictions = ref<AddictionItem[]>([])
const activeAddiction = ref<AddictionItem | null>(null)
const triggerVisible = ref(false)
const resetVisible = ref(false)

onMounted(async () => {
  addictions.value = await addictionsApi.getAddictions()
})

function onTrigger(addiction: AddictionItem) {
  activeAddiction.value = addiction
  triggerVisible.value = true
}

function onReset(addiction: AddictionItem) {
  activeAddiction.value = addiction
  resetVisible.value = true
}

function handleReset(payload: { addiction: AddictionItem; note: string; }) {
  resetVisible.value = false;
  payload.addiction.lastReset = new Date();
  // todo: store notes in the journal
}
</script>

<template>
  <AddictionCard v-for="addiction in addictions" :key="addiction.id" :addiction="addiction" @trigger="onTrigger"
    @reset="onReset" />
  <AddictionTriggerModal v-if="activeAddiction" v-model:visible="triggerVisible" :addiction="activeAddiction"
    @close="triggerVisible = false" />
  <AddictionResetModal v-if="activeAddiction" v-model:visible="resetVisible" :addiction="activeAddiction"
    @close="resetVisible = false" @confirm="handleReset" />
</template>

<style scoped>
.tasks-list {
  margin: 12px;
}
</style>