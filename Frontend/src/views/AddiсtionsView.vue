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
    <h1 class="view-page-header">{{ $t('addictions.addictions') }}</h1>
    <AddictionCard v-for="a in addictionsStore.addictionsSorted" :key="a.addiction.id" :addiction="a"
      @edit="(addiction) => emit('edit-addiction', addiction)" />
  </div>
</template>

<style scoped>
.addictions-view {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 0 12px 12px;
}

.view-page-header {
  font-size: var(--p-card-title-font-size);
  font-weight: 600;
  text-align: center;
}
</style>
