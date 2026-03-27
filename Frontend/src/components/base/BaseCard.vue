<script setup lang="ts">
import Card from "primevue/card"
import { computed } from "vue"

const props = withDefaults(defineProps<{
  borderless?: boolean
  accentColor?: string | null
}>(), {
  borderless: false,
  accentColor: null
})

const cardStyle = computed(() => {
  if (props.borderless) return { border: "none", borderLeftWidth: 0 }
  if (!props.accentColor) return { borderLeftWidth: 0 }
  return {
    borderLeftWidth: "var(--ds-accent-border-width)",
    borderLeftStyle: "solid",
    borderLeftColor: props.accentColor
  }
})
</script>

<template>
  <Card class="ds-card" :class="{ 'ds-card--borderless': borderless }" :style="cardStyle">
    <template v-if="$slots.header" #header><slot name="header" /></template>
    <template v-if="$slots.title" #title><slot name="title" /></template>
    <template #content><slot name="content" /></template>
    <template v-if="$slots.footer" #footer><slot name="footer" /></template>
  </Card>
</template>

<style scoped>
.ds-card.ds-card--borderless :deep(.p-card) {
  border: none;
  box-shadow: none;
}
</style>
