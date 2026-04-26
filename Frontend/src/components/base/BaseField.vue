<script setup lang="ts">
defineProps<{
  label?: string
  hint?: string
  error?: string
}>()
</script>

<template>
  <div class="ds-drawer-section">
    <label v-if="label" class="ds-drawer-label">{{ label }}</label>
    <slot />
    <small v-if="hint && !error" class="base-field-hint">{{ hint }}</small>
    <small v-if="error" class="base-field-error">{{ error }}</small>
  </div>
</template>

<style scoped>
.base-field-hint {
  color: var(--p-text-muted-color);
  font-size: var(--ds-font-size-xs);
}

.base-field-error {
  color: var(--p-red-500);
  font-size: var(--ds-font-size-xs);
}
</style>
