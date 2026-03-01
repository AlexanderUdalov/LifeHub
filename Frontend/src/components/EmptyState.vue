<script setup lang="ts">
defineProps<{
  icon: string
  title: string
  subtitle?: string
}>()
</script>

<template>
  <div class="empty-state">
    <div class="empty-state-icon-wrap">
      <i :class="icon" />
    </div>
    <p class="empty-state-title">{{ title }}</p>
    <p v-if="subtitle" class="empty-state-subtitle">{{ subtitle }}</p>
  </div>
</template>

<style scoped>
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  padding: 2.5rem 1.5rem;
}

.empty-state-icon-wrap {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 3.5rem;
  height: 3.5rem;
  border-radius: 50%;
  background: color-mix(in srgb, var(--p-primary-color) 12%, transparent);
  margin-bottom: 0.25rem;
}

.empty-state-icon-wrap i {
  font-size: 1.5rem;
  color: var(--p-primary-color);
}

.empty-state-title {
  font-size: 1.05rem;
  font-weight: 600;
  color: var(--p-text-color);
  margin: 0;
  text-align: center;
}

.empty-state-subtitle {
  font-size: 0.875rem;
  color: var(--p-text-muted-color);
  margin: 0;
  text-align: center;
  max-width: 260px;
  line-height: 1.45;
}
</style>
