<script setup lang="ts">
import AddictionCard from '@/components/AddictionCard.vue'
import { onMounted } from 'vue'
import { useAddictionsStore } from '@/stores/addictions'
import type { AddictionDTO } from '@/api/AddictionsAPI'

const addictionsStore = useAddictionsStore()

const emit = defineEmits<{
  (e: 'edit-addiction', addiction: AddictionDTO): void
}>()

onMounted(async () => {
  await addictionsStore.fetchAddictions(60)
})
</script>

<template>
  <div class="addictions-view">
    <AddictionCard v-for="a in addictionsStore.addictionsSorted" :key="a.addiction.id" :addiction="a"
      @edit="(addiction) => emit('edit-addiction', addiction)" />
  </div>
</template>

<style scoped>
.addictions-view {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 12px;
}
</style>
