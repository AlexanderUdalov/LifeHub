<script setup lang="ts">
import BaseDrawer from '@/components/base/BaseDrawer.vue'
import DateAndRecurrencePicker from '@/components/DateAndRecurrencePicker.vue'
import { useI18n } from 'vue-i18n'

defineProps<{
  visible: boolean
  date: Date | null
  recurrenceRule: string | null
}>()

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void
  (e: 'update:date', value: Date | null): void
  (e: 'update:recurrenceRule', value: string | null): void
}>()

const { t } = useI18n()

function onClosePicker() {
  emit('update:visible', false)
}
</script>

<template>
  <BaseDrawer
    :visible="visible"
    class="task-date-recurrence-drawer"
    min-height="min(90vh, 42rem)"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="task-date-recurrence-drawer-header">
        {{ t('tasks.recurrence.dateAndRepeat') }}
      </div>
    </template>

    <div class="task-date-recurrence-drawer-body">
      <DateAndRecurrencePicker
        :date="date"
        :recurrence-rule="recurrenceRule"
        @update:date="emit('update:date', $event)"
        @update:recurrence-rule="emit('update:recurrenceRule', $event)"
        @close="onClosePicker"
      />
    </div>
  </BaseDrawer>
</template>

<style scoped>
.task-date-recurrence-drawer-header {
  font-size: var(--ds-font-size-lg);
  font-weight: 600;
  width: 100%;
  text-align: center;
}

.task-date-recurrence-drawer-body {
  display: flex;
  flex-direction: column;
  gap: var(--ds-space-2);
  width: 100%;
  max-width: 28rem;
  margin: 0 auto;
  padding: 0 var(--ds-space-2);
  box-sizing: border-box;
}
</style>

<style>
/* Nested drawer above task edit drawer */
.task-date-recurrence-drawer.ds-bottom-drawer {
  z-index: 1301;
}
</style>
