<script setup lang="ts">
defineOptions({ name: 'AddictionsView' })
import AddictionCard from '@/components/AddictionCard.vue'
import EmptyState from '@/components/EmptyState.vue'
import Button from 'primevue/button'
import Skeleton from 'primevue/skeleton'
import { onMounted } from 'vue'
import { useAddictionsStore } from '@/stores/addictions'
import type { AddictionDTO } from '@/api/AddictionsAPI'

const addictionsStore = useAddictionsStore()

const emit = defineEmits<{
  (e: 'edit-addiction', addiction: AddictionDTO | null): void
}>()

onMounted(async () => {
  await addictionsStore.fetchAddictions(60)
})
</script>

<template>
  <div class="addictions-view">
    <header class="addictions-view__header">
      <h1 class="ds-page-header">{{ $t('addictions.addictions') }}</h1>
      <Button :label="$t('addictions.editdialog.newAddiction')" icon="pi pi-plus" class="desktop-create-btn"
        @click="emit('edit-addiction', null)" />
    </header>

    <div v-if="addictionsStore.isLoading && addictionsStore.addictions.length === 0" class="addictions-skeleton">
      <div v-for="i in 4" :key="i" class="skeleton-card">
        <Skeleton shape="circle" size="2.5rem" class="skeleton-avatar" />
        <div class="skeleton-lines">
          <Skeleton width="70%" height="1.25rem" />
          <Skeleton width="50%" height="1rem" />
        </div>
      </div>
    </div>

    <EmptyState v-else-if="addictionsStore.addictionsSortedVisible.length === 0" icon="pi pi-shield"
      :title="$t('addictions.empty')" :subtitle="$t('addictions.emptySubtitle')" />

    <AddictionCard v-else v-for="a in addictionsStore.addictionsSortedVisible" :key="a.addiction.id" :addiction="a"
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

.addictions-view__header {
  display: contents;
}

.desktop-create-btn {
  display: none;
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

@media (min-width: 900px) {
  .addictions-view {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(22rem, 1fr));
    align-items: start;
    gap: 1rem;
    max-width: 72rem;
    margin: 0 auto;
    padding: 0;
  }

  .addictions-view__header,
  .addictions-skeleton,
  .addictions-view :deep(.empty-state) {
    grid-column: 1 / -1;
  }

  .addictions-view__header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
  }

  .desktop-create-btn {
    display: inline-flex;
  }

  .addictions-view :deep(.empty-state) {
    justify-self: center;
  }

  .addictions-skeleton {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(22rem, 1fr));
    gap: 1rem;
  }
}
</style>
