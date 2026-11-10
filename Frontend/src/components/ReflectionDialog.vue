<script setup lang="ts">
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import ProgressBar from 'primevue/progressbar'
import Message from 'primevue/message'
import Dialog from 'primevue/dialog'
import { computed, nextTick, ref, watch, TransitionGroup } from 'vue'
import { useI18n } from 'vue-i18n'
import { useJournalStore } from '@/stores/journal'
import {
  startReflection,
  sendReflectionMessage,
  type ChatMessageDTO
} from '@/api/ReflectionAPI'
import DayFeedbackCard from '@/components/DayFeedbackCard.vue'

const { t, locale } = useI18n()
const journalStore = useJournalStore()

const emit = defineEmits<{
  (e: 'close'): void
}>()

const visible = ref(true)
watch(visible, (v) => {
  if (!v) emit('close')
})

type Step = 'period' | 'day' | 'chat' | 'complete'
const step = ref<Step>('period')

const contextId = ref('')
const messages = ref<ChatMessageDTO[]>([])
const journalSummary = ref<string | null>(null)
const userInput = ref('')
const isLoading = ref(false)
const errorText = ref('')
const isSaving = ref(false)
const maxSteps = 5
const selectedPeriod = ref(7)
const initialMessage = ref('')
const replyFailed = ref(false)

const messagesContainer = ref<HTMLElement | null>(null)
const inputEl = ref<HTMLElement | null>(null)

const userMessageCount = computed(() =>
  messages.value.filter((m) => m.role === 'user').length
)

const progress = computed(() =>
  Math.min(100, (userMessageCount.value / maxSteps) * 100)
)

function scrollToBottom() {
  nextTick(() => {
    if (messagesContainer.value) {
      messagesContainer.value.scrollTo({
        top: messagesContainer.value.scrollHeight,
        behavior: 'smooth'
      })
    }
  })
}

function focusInput() {
  nextTick(() => {
    inputEl.value?.querySelector('input')?.focus()
  })
}

async function selectPeriod(days: number, openingMessage = '') {
  selectedPeriod.value = days
  initialMessage.value = openingMessage
  contextId.value = ''
  messages.value = []
  journalSummary.value = null
  replyFailed.value = false
  step.value = 'chat'
  isLoading.value = true
  errorText.value = ''

  try {
    const language = locale.value === 'en' ? 'en' : 'ru'
    const res = await startReflection(days, language)
    contextId.value = res.contextId!
    messages.value = [{ role: 'assistant', content: res.message ?? '' }]
    if (openingMessage) {
      messages.value.push({ role: 'user', content: openingMessage })
      await requestReply()
    }
    scrollToBottom()
    focusInput()
  } catch {
    errorText.value = t('reflection.error')
  } finally {
    isLoading.value = false
  }
}

function discussDay(feedback: { score: number; note: string }) {
  const openingMessage = [
    t('dayFeedback.chatScore', { score: feedback.score }),
    feedback.note
  ].filter(Boolean).join('\n\n')
  selectPeriod(1, openingMessage)
}

async function sendMessage() {
  const text = userInput.value.trim()
  if (!text || isLoading.value) return

  messages.value.push({ role: 'user', content: text })
  userInput.value = ''
  await requestReply()
}

async function requestReply() {
  isLoading.value = true
  errorText.value = ''
  replyFailed.value = false
  scrollToBottom()

  try {
    const res = await sendReflectionMessage(contextId.value, messages.value)
    messages.value.push({ role: 'assistant', content: res.message ?? '' })
    if (res.journalSummary) journalSummary.value = res.journalSummary
    if (res.isComplete) step.value = 'complete'

    scrollToBottom()
    focusInput()
  } catch {
    errorText.value = t('reflection.error')
    replyFailed.value = true
  } finally {
    isLoading.value = false
  }
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter' && !e.shiftKey) {
    e.preventDefault()
    sendMessage()
  }
}

function getJournalText(): string {
  return journalSummary.value?.trim() ?? ''
}

async function saveToJournal() {
  const text = getJournalText()
  if (!text) {
    errorText.value = t('reflection.noSummary')
    return
  }
  isSaving.value = true
  errorText.value = ''

  try {
    await journalStore.createEntry({
      text,
      taskItemId: null,
      habitId: null,
      addictionId: null,
      goalId: null,
      lifeAreaId: null,
      aiGenerated: true
    })
    emit('close')
  } catch {
    errorText.value = t('reflection.error')
  } finally {
    isSaving.value = false
  }
}
</script>

<template>
  <Dialog v-model:visible="visible" modal :maximized="true" class="reflection-dialog">
    <template #header>
      <span class="reflection-dialog__title">
        <Button v-if="step === 'day'" icon="pi pi-arrow-left" variant="text" rounded
          :aria-label="t('reflection.backToPeriods')" @click="step = 'period'" />
        <i class="pi pi-sparkles" />
        {{ t('reflection.title') }}
      </span>
    </template>

    <!-- Period selection -->
    <div v-if="step === 'period'" class="reflection-period">
      <p class="reflection-period__question">{{ t('reflection.selectPeriod') }}</p>
      <div class="reflection-period__buttons">
        <Button
          :label="t('reflection.period1')"
          severity="secondary"
          outlined
          @click="step = 'day'"
        />
        <Button
          :label="t('reflection.period7')"
          severity="secondary"
          outlined
          @click="selectPeriod(7)"
        />
        <Button
          :label="t('reflection.period30')"
          severity="secondary"
          outlined
          @click="selectPeriod(30)"
        />
        <Button
          :label="t('reflection.period365')"
          severity="secondary"
          outlined
          @click="selectPeriod(365)"
        />
      </div>
    </div>

    <DayFeedbackCard v-if="step === 'day'" @discuss="discussDay" />

    <!-- Chat -->
    <template v-if="step === 'chat' || step === 'complete'">
      <ProgressBar :value="progress" :showValue="false" class="reflection-progress" />

      <div ref="messagesContainer" class="reflection-messages">
        <TransitionGroup name="reflection-msg" tag="div" class="reflection-messages__list">
          <div
            v-for="(msg, idx) in messages"
            :key="`msg-${idx}`"
            class="reflection-message"
            :class="{
              'reflection-message--ai': msg.role === 'assistant',
              'reflection-message--user': msg.role === 'user'
            }"
          >
            <div class="reflection-message__bubble">
              {{ msg.content }}
            </div>
          </div>

          <div v-if="isLoading" key="typing" class="reflection-message reflection-message--ai"
            role="status" :aria-label="t('reflection.loading')">
          <div class="reflection-message__bubble reflection-typing">
            <span class="reflection-typing__dot" />
            <span class="reflection-typing__dot" />
            <span class="reflection-typing__dot" />
          </div>
          </div>
        </TransitionGroup>
      </div>

      <Message v-if="errorText" severity="error" :closable="false" class="reflection-error">
        {{ errorText }}
        <Button v-if="!contextId || replyFailed" :label="t('dayFeedback.retry')" variant="text"
          :disabled="isLoading"
          @click="contextId ? requestReply() : selectPeriod(selectedPeriod, initialMessage)" />
      </Message>
    </template>

    <template v-if="step === 'chat' || step === 'complete'" #footer>
      <!-- Chat input -->
      <div v-if="step === 'chat'" class="reflection-input">
        <div ref="inputEl" class="reflection-input__wrap">
          <InputText
            v-model="userInput"
            :placeholder="t('reflection.inputPlaceholder')"
            class="reflection-input__field"
            :disabled="isLoading || !contextId || replyFailed"
            @keydown="handleKeydown"
          />
          <Button
            icon="pi pi-send"
            rounded
            :disabled="!userInput.trim() || isLoading || !contextId || replyFailed"
            :aria-label="t('reflection.send')"
            class="reflection-input__send"
            @click="sendMessage"
          />
        </div>
      </div>

      <!-- Complete actions -->
      <div v-if="step === 'complete'" class="reflection-complete">
        <p class="reflection-complete__label">{{ t('reflection.complete') }}</p>
        <div class="reflection-complete__actions">
          <Button
            :label="t('reflection.saveToJournal')"
            icon="pi pi-book"
            :loading="isSaving"
            :disabled="!getJournalText()"
            @click="saveToJournal"
          />
          <Button
            :label="t('cancel')"
            severity="secondary"
            variant="text"
            @click="emit('close')"
          />
        </div>
      </div>
    </template>
  </Dialog>
</template>

<style>

.reflection-dialog__title {
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--p-text-color);
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.reflection-dialog__title .pi-sparkles {
  color: var(--p-primary-color);
}

.reflection-dialog.p-dialog {
  background: var(--p-content-background);
  height: 100dvh;
  max-height: 100dvh;
  width: 100%;
  margin: 0;
  border: 0;
  border-radius: 0;
}

.reflection-dialog .p-dialog-header {
  flex-shrink: 0;
  padding: max(0.75rem, env(safe-area-inset-top, 0px)) max(1.25rem, env(safe-area-inset-right, 0px)) 0.75rem max(1.25rem, env(safe-area-inset-left, 0px));
  background: var(--p-content-background);
  border-bottom: 1px solid var(--p-content-border-color);
}

.reflection-dialog .p-dialog-content {
  display: flex;
  flex-direction: column;
  flex: 1;
  min-height: 0;
  gap: 0.75rem;
  padding: 1rem max(1.25rem, env(safe-area-inset-right, 0px)) max(1rem, env(safe-area-inset-bottom, 0px)) max(1.25rem, env(safe-area-inset-left, 0px));
}

.reflection-dialog:has(.reflection-messages) .p-dialog-content {
  overflow: hidden;
  padding-bottom: 0.5rem;
}

.reflection-dialog .p-dialog-footer {
  flex-shrink: 0;
  border-top: 1px solid var(--p-content-border-color);
  padding: 0.75rem max(1.25rem, env(safe-area-inset-right, 0px)) max(0.75rem, env(safe-area-inset-bottom, 0px)) max(1.25rem, env(safe-area-inset-left, 0px));
  display: block;
}

.reflection-dialog .day-feedback,
.reflection-period {
  width: 100%;
  max-width: 42rem;
  align-self: center;
  box-sizing: border-box;
}

/* Period selection */
.reflection-period {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1.5rem;
  padding: 2rem 0;
}

.reflection-period__question {
  font-size: 1.125rem;
  font-weight: 500;
  color: var(--p-text-color);
  text-align: center;
  margin: 0;
}

.reflection-period__buttons {
  display: flex;
  gap: 0.75rem;
  flex-wrap: wrap;
  justify-content: center;
}

/* Progress */
.reflection-progress {
  flex-shrink: 0;
}

.reflection-progress.p-progressbar {
  height: 0.25rem;
  border-radius: 999px;
}

/* Messages */
.reflection-messages {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  padding: 0.5rem 0;
}

.reflection-messages__list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

/* New message slide-up animation */
.reflection-msg-enter-active {
  transition: transform 0.25s ease-out, opacity 0.2s ease-out;
}

.reflection-msg-enter-from {
  transform: translateY(0.75rem);
  opacity: 0;
}

.reflection-msg-move {
  transition: transform 0.3s ease;
}

.reflection-message {
  display: flex;
  max-width: 85%;
}

.reflection-message--ai {
  align-self: flex-start;
}

.reflection-message--user {
  align-self: flex-end;
}

.reflection-message__bubble {
  padding: 0.625rem 0.875rem;
  border-radius: 1rem;
  font-size: 0.9375rem;
  line-height: 1.5;
  white-space: pre-wrap;
  word-break: break-word;
}

.reflection-message--ai .reflection-message__bubble {
  background: color-mix(in srgb, var(--p-card-background) 85%, var(--p-surface-0));
  color: var(--p-text-color);
  border: 1px solid var(--p-card-border-color);
  border-bottom-left-radius: 0.25rem;
}

.reflection-message--user .reflection-message__bubble {
  background: var(--p-primary-color);
  color: var(--p-primary-contrast-color);
  border: 1px solid var(--p-primary-color);
  border-bottom-right-radius: 0.25rem;
}

/* Typing indicator */
.reflection-typing {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.75rem 1rem;
}

.reflection-typing__dot {
  width: 0.5rem;
  height: 0.5rem;
  border-radius: 50%;
  background: var(--p-text-muted-color);
  animation: reflection-bounce 1.4s ease-in-out infinite;
}

.reflection-typing__dot:nth-child(2) {
  animation-delay: 0.2s;
}

.reflection-typing__dot:nth-child(3) {
  animation-delay: 0.4s;
}

@keyframes reflection-bounce {
  0%, 60%, 100% {
    transform: translateY(0);
    opacity: 0.4;
  }
  30% {
    transform: translateY(-0.375rem);
    opacity: 1;
  }
}

/* Input */
.reflection-input__wrap {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.reflection-input__field {
  flex: 1;
  min-width: 0;
  font-size: 1rem;
}

.reflection-input__send {
  flex-shrink: 0;
  width: 2.5rem;
  height: 2.5rem;
}

/* Complete */
.reflection-complete {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.reflection-complete__label {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--p-text-color);
  margin: 0;
  text-align: center;
}

.reflection-complete__actions {
  display: flex;
  gap: 0.5rem;
  justify-content: center;
}

/* Error */
.reflection-error {
  flex-shrink: 0;
}
</style>
