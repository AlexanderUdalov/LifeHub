<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import Button from 'primevue/button'
import Carousel from 'primevue/carousel'
import Textarea from 'primevue/textarea'
import Message from 'primevue/message'
import { useI18n } from 'vue-i18n'
import BaseDrawer from '@/components/base/BaseDrawer.vue'
import type { AddictionDTO, AddictionTriggerOutcome, GenerateTriggerGuidanceResponse } from '@/api/AddictionsAPI'
import { addictionsApi } from '@/api/AddictionsAPI'
import { useAddictionsStore } from '@/stores/addictions'
import { useApiError } from '@/composables/useApiError'

const props = defineProps<{
  addiction: AddictionDTO
}>()

const emit = defineEmits<{
  (e: 'close'): void
}>()

const { t, locale } = useI18n()
const addictionsStore = useAddictionsStore()
const apiError = useApiError()

const visible = ref(true)
watch(visible, (v) => {
  if (!v) emit('close')
})

const guidance = ref<GenerateTriggerGuidanceResponse | null>(null)
const guidanceError = ref('')
const isGuidanceLoading = ref(true)

const triggerNote = ref('')
const isSubmittingOutcome = ref(false)
const submitError = ref('')

const slides = computed(() => {
  const tips = guidance.value?.tips
  if (tips?.length) {
    return tips.map((text, idx) => ({
      icon: ['pi pi-clock', 'pi pi-chart-line', 'pi pi-bolt', 'pi pi-star', 'pi pi-heart'][idx] ?? 'pi pi-bolt',
      text
    }))
  }
  return [
    { icon: 'pi pi-clock', text: t('addictions.triggerDrawer.slide1') },
    { icon: 'pi pi-chart-line', text: t('addictions.triggerDrawer.slide2') },
    { icon: 'pi pi-bolt', text: t('addictions.triggerDrawer.slide3') },
    { icon: 'pi pi-star', text: t('addictions.triggerDrawer.slide4') },
    { icon: 'pi pi-heart', text: t('addictions.triggerDrawer.slide5') }
  ]
})

const titleText = computed(() => guidance.value?.title || t('addictions.triggerDrawer.title'))
const subtitleText = computed(() => guidance.value?.subtitle || t('addictions.triggerDrawer.subtitle'))

async function loadGuidance() {
  guidanceError.value = ''
  isGuidanceLoading.value = true
  try {
    guidance.value = await addictionsApi.getTriggerGuidance(props.addiction.id, locale.value)
  } catch (e) {
    guidanceError.value = apiError.resolveMessage(e)
  } finally {
    isGuidanceLoading.value = false
  }
}

watch(
  [() => props.addiction.id, () => locale.value],
  () => {
    void loadGuidance()
  },
  { immediate: true }
)

async function submitOutcome(outcome: AddictionTriggerOutcome) {
  submitError.value = ''
  isSubmittingOutcome.value = true
  try {
    await addictionsStore.logTriggerEvent(props.addiction.id, outcome, {
      note: triggerNote.value.trim() || null,
      eventAt: new Date().toISOString()
    })
    visible.value = false
  } catch (e) {
    submitError.value = apiError.resolveMessage(e)
  } finally {
    isSubmittingOutcome.value = false
  }
}
</script>

<template>
  <BaseDrawer
    v-model:visible="visible"
    class="addiction-drawer"
  >
    <template #header>
      <div class="trigger-drawer-title-row">
        <i class="pi pi-bolt trigger-title-icon" />
        <span>{{ titleText }}</span>
      </div>
    </template>

    <p class="trigger-subtitle">
      {{ subtitleText }}
    </p>

    <p class="trigger-addiction-name">{{ addiction.title }}</p>

    <Message v-if="guidanceError" severity="warn" icon="pi pi-exclamation-triangle">
      {{ guidanceError }}
    </Message>

    <Carousel :value="slides" :numVisible="1" :numScroll="1" circular :autoplayInterval="5000">
      <template #item="{ data }">
        <div class="trigger-slide">
          <i :class="data.icon" class="trigger-slide-icon" />
          <p class="trigger-slide-text">{{ data.text }}</p>
        </div>
      </template>
    </Carousel>

    <div class="addiction-drawer-section trigger-note-section">
      <label class="addiction-drawer-label">{{ t('addictions.triggerDrawer.note') }}</label>
      <Textarea
        v-model="triggerNote"
        class="trigger-note-textarea w-full"
        :placeholder="t('addictions.triggerDrawer.notePlaceholder')"
        autoResize
        rows="4"
      />
    </div>

    <Message v-if="submitError" severity="error" icon="pi pi-times-circle">
      {{ submitError }}
    </Message>

    <template #footer>
      <div class="addiction-drawer-actions trigger-actions">
        <Button
          :label="t('addictions.triggerDrawer.overcame')"
          icon="pi pi-check"
          severity="success"
          :loading="isSubmittingOutcome || isGuidanceLoading"
          @click="submitOutcome(0)"
        />
        <Button
          :label="t('addictions.triggerDrawer.relapsed')"
          icon="pi pi-refresh"
          severity="danger"
          :loading="isSubmittingOutcome || isGuidanceLoading"
          @click="submitOutcome(1)"
        />
      </div>
    </template>
  </BaseDrawer>
</template>

<style>
.trigger-drawer-title-row {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  font-size: 1.1rem;
}

.trigger-title-icon {
  color: var(--p-warning-color);
}

.trigger-subtitle {
  color: var(--p-text-muted-color);
  font-size: 0.95rem;
  margin-bottom: 16px;
  line-height: 1.4;
}

.trigger-addiction-name {
  margin: 0 0 8px;
  font-size: 0.95rem;
  font-weight: 600;
}

.trigger-slide {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 1.5rem 1rem;
  text-align: center;
  gap: 12px;
}

.trigger-slide-icon {
  font-size: 2rem;
  color: var(--p-warning-color);
}

.trigger-slide-text {
  font-size: 1.05rem;
  line-height: 1.5;
  max-width: 300px;
}

.trigger-actions {
  justify-content: space-between;
}

.trigger-note-section {
  width: 100%;
}

.trigger-note-textarea {
  width: 100%;
  min-height: 6rem;
}
</style>
