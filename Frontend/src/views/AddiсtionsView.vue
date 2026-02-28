<script setup lang="ts">
import AddictionCard from '@/components/AddictionCard.vue'
import Skeleton from 'primevue/skeleton'
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

    <div v-if="addictionsStore.isLoading" class="addictions-skeleton">
      <div v-for="i in 4" :key="i" class="skeleton-card">
        <Skeleton shape="circle" size="2.5rem" class="skeleton-avatar" />
        <div class="skeleton-lines">
          <Skeleton width="70%" height="1.25rem" />
          <Skeleton width="50%" height="1rem" />
        </div>
      </div>
    </div>

    <div v-else-if="addictionsStore.addictions.length === 0" class="empty-placeholder">
      <p>{{ $t('addictions.empty') }}</p>
    </div>

    <AddictionCard v-else v-for="a in addictionsStore.addictionsSorted" :key="a.addiction.id" :addiction="a"
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

.addictions-skeleton {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.addictions-view .skeleton-card {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem;
  border-radius: var(--p-border-radius);
  background: var(--p-card-background);
  border: 1px solid var(--p-card-border-color);
}

.addictions-view .skeleton-avatar {
  flex-shrink: 0;
}

.addictions-view .skeleton-lines {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  flex: 1;
}

.empty-placeholder {
  text-align: center;
  color: var(--p-text-muted-color);
}

.view-page-header {
  font-size: var(--p-card-title-font-size);
  font-weight: 600;
  text-align: center;
}
</style>
