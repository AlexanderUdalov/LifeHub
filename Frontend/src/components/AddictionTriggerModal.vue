<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import Button from 'primevue/button'
import Carousel from 'primevue/carousel'
import Textarea from 'primevue/textarea'
import Message from 'primevue/message'
import ProgressSpinner from 'primevue/progressspinner'
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

const noteVisible = ref(false)
const pendingOutcome = ref<AddictionTriggerOutcome | null>(null)
const triggerNote = ref('')
const isSubmittingOutcome = ref(false)
const submitError = ref('')

function isImageUrl(value?: string | null): boolean {
  if (!value) return false
  return /^https?:\/\//i.test(value.trim())
}

const slides = computed(() => {
  const aiSlides = guidance.value?.slides
    ?.filter((x) => x?.text?.trim())
    .map((x) => ({
      icon: 'pi pi-star',
      text: x.text.trim(),
      image: x.image?.trim() || null
    }))

  if (aiSlides && aiSlides.length > 0) {
    return aiSlides
  }

  const tips = guidance.value?.tips
  const motivation = guidance.value?.subtitle?.trim()
  const motivationSlide = motivation
    ? [
        {
          icon: 'pi pi-heart',
          text: motivation
        }
      ]
    : []

  if (tips?.length) {
    return [...motivationSlide, ...tips.map((text, idx) => ({
      icon: ['pi pi-clock', 'pi pi-chart-line', 'pi pi-bolt', 'pi pi-star', 'pi pi-heart'][idx] ?? 'pi pi-bolt',
      text
    }))]
  }
  return [
    ...motivationSlide,
    { icon: 'pi pi-clock', text: t('addictions.triggerDrawer.slide1') },
    { icon: 'pi pi-chart-line', text: t('addictions.triggerDrawer.slide2') },
    { icon: 'pi pi-bolt', text: t('addictions.triggerDrawer.slide3') },
    { icon: 'pi pi-star', text: t('addictions.triggerDrawer.slide4') },
    { icon: 'pi pi-heart', text: t('addictions.triggerDrawer.slide5') }
  ]
})

const titleText = computed(() => guidance.value?.title || t('addictions.triggerDrawer.title'))
const subtitleText = computed(() => t('addictions.triggerDrawer.subtitle'))

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

async function submitOutcome(outcome: AddictionTriggerOutcome, note: string) {
  submitError.value = ''
  isSubmittingOutcome.value = true
  try {
    await addictionsStore.logTriggerEvent(props.addiction.id, outcome, {
      note: note.trim() || null,
      eventAt: new Date().toISOString(),
      language: locale.value
    })
    triggerNote.value = ''
    noteVisible.value = false
    pendingOutcome.value = null
    visible.value = false
  } catch (e) {
    submitError.value = apiError.resolveMessage(e)
  } finally {
    isSubmittingOutcome.value = false
  }
}

function openNoteStep(outcome: AddictionTriggerOutcome) {
  pendingOutcome.value = outcome
  noteVisible.value = true
}

function closeNoteStep() {
  noteVisible.value = false
  pendingOutcome.value = null
}

async function confirmWithNote() {
  if (pendingOutcome.value === null) return
  await submitOutcome(pendingOutcome.value, triggerNote.value)
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

    <Message v-if="guidanceError" severity="warn" icon="pi pi-exclamation-triangle">
      {{ guidanceError }}
    </Message>

    <div v-if="isGuidanceLoading" class="trigger-loading">
      <ProgressSpinner strokeWidth="6" style="width: 2.5rem; height: 2.5rem" />
      <p class="trigger-loading-text">{{ t('addictions.triggerDrawer.loading') }}</p>
    </div>
    <Carousel v-else :value="slides" :numVisible="1" :numScroll="1" circular :autoplayInterval="5000">
      <template #item="{ data }">
        <div class="trigger-slide">
          <img v-if="isImageUrl(data.image)" :src="data.image" alt="" class="trigger-slide-image" />
          <div v-else-if="data.image" class="trigger-slide-emoji">{{ data.image }}</div>
          <i v-else :class="data.icon" class="trigger-slide-icon" />
          <p class="trigger-slide-text">{{ data.text }}</p>
        </div>
      </template>
    </Carousel>

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
          @click="openNoteStep(0)"
        />
        <Button
          :label="t('addictions.triggerDrawer.relapsed')"
          icon="pi pi-refresh"
          severity="danger"
          :loading="isSubmittingOutcome || isGuidanceLoading"
          @click="openNoteStep(1)"
        />
      </div>
    </template>
  </BaseDrawer>

  <BaseDrawer
    v-model:visible="noteVisible"
    class="addiction-drawer"
    max-height="68vh"
  >
    <template #header>
      <div class="trigger-drawer-title-row">
        <i class="pi pi-pencil trigger-title-icon" />
        <span>{{ t('addictions.triggerDrawer.noteStepTitle') }}</span>
      </div>
    </template>

    <div class="addiction-drawer-section trigger-note-section">
      <label class="addiction-drawer-label">{{ t('addictions.triggerDrawer.note') }}</label>
      <Textarea
        v-model="triggerNote"
        class="trigger-note-textarea w-full"
        :placeholder="t('addictions.triggerDrawer.notePlaceholder')"
        autoResize
        rows="5"
      />
    </div>

    <template #footer>
      <div class="addiction-drawer-actions trigger-actions">
        <Button :label="t('cancel')" severity="secondary" text @click="closeNoteStep" />
        <Button
          :label="t('addictions.triggerDrawer.saveAction')"
          icon="pi pi-check"
          :loading="isSubmittingOutcome"
          @click="confirmWithNote"
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
  margin-bottom: 12px;
  line-height: 1.4;
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

.trigger-slide-image {
  width: 96px;
  height: 96px;
  object-fit: cover;
  border-radius: 16px;
  border: 1px solid var(--p-content-border-color);
}

.trigger-slide-emoji {
  font-size: 2.8rem;
  line-height: 1;
}

.trigger-actions {
  justify-content: flex-end;
  gap: 8px;
}

.trigger-note-section {
  width: 100%;
}

.trigger-note-textarea {
  width: 100%;
  min-height: 6rem;
}

.trigger-loading {
  min-height: 220px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 12px;
}

.trigger-loading-text {
  margin: 0;
  color: var(--p-text-muted-color);
  font-size: 0.9rem;
}
</style>
