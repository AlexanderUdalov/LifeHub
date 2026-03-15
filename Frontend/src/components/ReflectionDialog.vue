<script setup lang="ts">
import Drawer from 'primevue/drawer'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import ProgressBar from 'primevue/progressbar'
import ProgressSpinner from 'primevue/progressspinner'
import Message from 'primevue/message'
import { computed, nextTick, ref, watch, TransitionGroup } from 'vue'
import { useI18n } from 'vue-i18n'
import { useJournalStore } from '@/stores/journal'
import {
  startReflection,
  sendReflectionMessage,
  type ChatMessageDTO
} from '@/api/ReflectionAPI'

const { t } = useI18n()
const journalStore = useJournalStore()

const emit = defineEmits<{
  (e: 'close'): void
}>()

const visible = ref(true)
watch(visible, (v) => {
  if (!v) emit('close')
})

type Step = 'period' | 'chat' | 'complete'
const step = ref<Step>('period')

const contextId = ref('')
const messages = ref<ChatMessageDTO[]>([])
const journalSummary = ref<string | null>(null)
const userInput = ref('')
const isLoading = ref(false)
const errorText = ref('')
const isSaving = ref(false)
const maxSteps = 5

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

async function selectPeriod(days: number) {
  isLoading.value = true
  errorText.value = ''

  try {
    const res = await startReflection(days)
    contextId.value = res.contextId!
    messages.value = [{ role: 'assistant', content: res.message ?? '' }]
    journalSummary.value = null
    step.value = 'chat'
    scrollToBottom()
    focusInput()
  } catch {
    errorText.value = t('reflection.error')
  } finally {
    isLoading.value = false
  }
}

async function sendMessage() {
  const text = userInput.value.trim()
  if (!text || isLoading.value) return

  messages.value.push({ role: 'user', content: text })
  userInput.value = ''
  isLoading.value = true
  errorText.value = ''
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
      lifeAreaId: null
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
  <Drawer v-model:visible="visible" position="bottom" class="reflection-drawer" style="height: 85vh">
    <template #header>
      <span class="reflection-drawer__title">
        <i class="pi pi-sparkles" />
        {{ t('reflection.title') }}
      </span>
    </template>

    <!-- Period selection -->
    <div v-if="step === 'period'" class="reflection-period">
      <p class="reflection-period__question">{{ t('reflection.selectPeriod') }}</p>
      <div v-if="isLoading" class="reflection-period__loading">
        <ProgressSpinner style="width: 2.5rem; height: 2.5rem" strokeWidth="4" />
        <span class="reflection-period__loading-text">{{ t('reflection.loading') }}</span>
      </div>
      <div v-else class="reflection-period__buttons">
        <Button
          :label="t('reflection.period7')"
          severity="secondary"
          outlined
          @click="selectPeriod(7)"
        />
        <Button
          :label="t('reflection.period14')"
          severity="secondary"
          outlined
          @click="selectPeriod(14)"
        />
        <Button
          :label="t('reflection.period30')"
          severity="secondary"
          outlined
          @click="selectPeriod(30)"
        />
      </div>

      <Message v-if="errorText" severity="error" :closable="false" class="reflection-error">
        {{ errorText }}
      </Message>
    </div>

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

          <div v-if="isLoading" key="typing" class="reflection-message reflection-message--ai">
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
      </Message>
    </template>

    <template #footer>
      <!-- Chat input -->
      <div v-if="step === 'chat'" class="reflection-input">
        <div ref="inputEl" class="reflection-input__wrap">
          <InputText
            v-model="userInput"
            :placeholder="t('reflection.inputPlaceholder')"
            class="reflection-input__field"
            :disabled="isLoading"
            @keydown="handleKeydown"
          />
          <Button
            icon="pi pi-send"
            rounded
            :disabled="!userInput.trim() || isLoading"
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
  </Drawer>
</template>

<style>
.p-drawer.reflection-drawer {
  border-radius: 1rem 1rem 0 0;
}

.reflection-drawer .p-drawer-header {
  position: relative;
  padding: 0.75rem 1.25rem;
  padding-top: 1.5rem;
}

.reflection-drawer .p-drawer-header::before {
  content: '';
  position: absolute;
  top: 0.5rem;
  left: 50%;
  transform: translateX(-50%);
  width: 2.5rem;
  height: 0.25rem;
  background: var(--p-content-border-color);
  border-radius: 999px;
}

.reflection-drawer__title {
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--p-text-color);
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.reflection-drawer__title .pi-sparkles {
  color: var(--p-primary-color);
}

.reflection-drawer .p-drawer-content {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding-bottom: 0.25rem;
  overflow: hidden;
}

.reflection-drawer .p-drawer-footer {
  border-top: 1px solid var(--p-content-border-color);
  padding: 0.75rem 1.25rem;
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

.reflection-period__loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
  padding: 1rem 0;
}

.reflection-period__loading-text {
  font-size: 0.875rem;
  color: var(--p-text-muted-color);
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
