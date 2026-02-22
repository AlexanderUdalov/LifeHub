<script setup lang="ts">
import Card from 'primevue/card'
import Button from 'primevue/button'
import AddictionDayRow from './AddictionDayRow.vue'
import type { AddictionDTO, AddictionWithResetsDTO } from '@/api/AddictionsAPI'
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { getTimeSince, fromDateOnlyString, toDateOnlyString, isSameDateOnly, startOfDay, parseUtcIso } from '@/utils/dateOnly'
import { useI18n } from 'vue-i18n'
import { useAddictionsStore } from '@/stores/addictions'

const props = defineProps<{ addiction: AddictionWithResetsDTO }>()

const emit = defineEmits<{
  (e: 'edit', addiction: AddictionDTO): void
}>()

const { t } = useI18n()
const addictionsStore = useAddictionsStore()

/** Updated every minute so the "time since" counter refreshes. */
const now = ref(new Date())
let tickInterval: ReturnType<typeof setInterval> | undefined
onMounted(() => {
  tickInterval = setInterval(() => {
    now.value = new Date()
  }, 60_000)
})
onUnmounted(() => {
  if (tickInterval) clearInterval(tickInterval)
})

function onReset() {
  addictionsStore.setReset(props.addiction.addiction.id, new Date())
}

/** Date of the most recent reset (last in list, since API returns ordered by ResetAt). */
const lastResetDateKey = computed(() =>
  props.addiction.resetDates.length ? props.addiction.resetDates[props.addiction.resetDates.length - 1]! : null
)

/** Reference moment for "time since": when last reset is today use lastResetAt (exact time), otherwise start of that day. */
const timeSinceRef = computed(() => {
  const key = lastResetDateKey.value
  if (!key) return null
  const lastResetAt = props.addiction.lastResetAt
  const refDate = fromDateOnlyString(key)
  if (lastResetAt && isSameDateOnly(parseUtcIso(lastResetAt), now.value)) {
    return parseUtcIso(lastResetAt)
  }
  return startOfDay(refDate)
})

const timeSince = computed(() => {
  const ref = timeSinceRef.value
  if (!ref) return null
  return getTimeSince(ref, now.value)
})

/** If days >= 5 show only days count; if days < 5 show full time (days, hours, minutes). */
const timeSinceText = computed(() => {
  const ts = timeSince.value
  if (!ts) return null
  if (ts.days >= 5) {
    return t('addictions.daysCount', { count: ts.days })
  }
  return t('addictions.timeSince', { days: ts.days, hours: ts.hours, minutes: ts.minutes })
})
</script>

<template>
  <Card class="addiction-card">
    <template #title>
      <div class="addiction-card-header">
        <div class="addiction-title" @click="emit('edit', addiction.addiction)">
          {{ addiction.addiction.title }}
        </div>
        <Button :label="t('addictions.reset')" icon="pi pi-undo" severity="danger" size="small" @click="onReset" />
      </div>
    </template>

    <template #content>
      <div v-if="timeSinceText" class="time-since">
        {{ timeSinceText }}
      </div>

      <AddictionDayRow :addiction="addiction" />
    </template>
  </Card>
</template>

<style scoped>
.addiction-card {
  border-radius: 16px;
}

.addiction-card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  width: 100%;
}

.addiction-title {
  cursor: pointer;
  user-select: none;
  flex: 1;
  min-width: 0;
}

.time-since {
  font-size: 0.875rem;
  color: var(--p-text-muted-color);
  margin-bottom: 8px;
}

.time-since.no-reset {
  font-style: italic;
}
</style>
