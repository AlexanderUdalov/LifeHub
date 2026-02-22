<script setup lang="ts">
import { computed } from 'vue'
import type { AddictionWithResetsDTO } from '@/api/AddictionsAPI'
import { toDateOnlyString } from '@/utils/dateOnly'

const props = defineProps<{
  date: Date
  addiction: AddictionWithResetsDTO
  strength: string
  resetCount: number
  disabled?: boolean
}>()

const isReset = computed(() => props.resetCount > 0)
</script>

<template>
  <div class="cell-wrapper">
    <div
      class="cell"
      :class="{ reset: isReset, disabled: disabled }"
      :style="{
        '--addiction-color': addiction.addiction.color,
        '--addiction-strength': strength
      }"
    >
      <i v-if="resetCount === 1" class="pi pi-times reset-icon" />
      <span v-else-if="resetCount > 1" class="reset-count">{{ resetCount }}</span>
    </div>
  </div>
</template>

<style scoped>
.cell-wrapper {
  display: flex;
  flex-shrink: 0;
}

.cell {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  margin: 2px;
  border-radius: 6px;
  border: 1px solid color-mix(in srgb, var(--addiction-color) var(--addiction-strength), transparent);
  flex-shrink: 0;
  transition: background-color 0.2s;
  background-color: color-mix(in srgb, var(--addiction-color) var(--addiction-strength), transparent);
}

.cell.reset {
  background-color: transparent;
  border-color: var(--red-500);
  color: var(--red-500);
}

.cell.disabled {
  opacity: 0.35;
  background-color: var(--p-content-border-color);
  border-color: var(--p-content-border-color);
}

.reset-icon {
  font-size: 1rem;
  font-weight: bold;
}

.reset-count {
  font-size: 0.75rem;
  font-weight: bold;
}
</style>
