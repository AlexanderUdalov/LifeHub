<script setup lang="ts">
defineOptions({ name: 'AddictionsView' })
import AddictionCard from '@/components/AddictionCard.vue'
import EmptyState from '@/components/EmptyState.vue'
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
    <h1 class="ds-page-header">{{ $t('addictions.addictions') }}</h1>

    <div v-if="addictionsStore.isLoading && addictionsStore.addictions.length === 0" class="addictions-skeleton">
      <div v-for="i in 4" :key="i" class="skeleton-card">
        <Skeleton shape="circle" size="2.5rem" class="skeleton-avatar" />
        <div class="skeleton-lines">
          <Skeleton width="70%" height="1.25rem" />
          <Skeleton width="50%" height="1rem" />
        </div>
      </div>
    </div>

    <EmptyState v-else-if="addictionsStore.addictions.length === 0" icon="pi pi-shield"
      :title="$t('addictions.empty')" :subtitle="$t('addictions.emptySubtitle')" />

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

</style>
