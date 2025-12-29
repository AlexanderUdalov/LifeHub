<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { addictionsApi } from '@/api/AddictionAPI'
import type { AddictionItem } from '@/models/AddictionItem'

import AddictionCard from '@/components/AddictionCard.vue'

const addictions = ref<AddictionItem[]>([])

onMounted(async () => {
    addictions.value = await addictionsApi.getAddictions()
})
</script>

<template>
    <AddictionCard v-for="addiction in addictions" :key="addiction.id" :addiction="addiction" />
</template>

<style scoped>
.tasks-list {
  margin: 12px;
}
</style>