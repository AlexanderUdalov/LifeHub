<script setup lang="ts">
defineOptions({ name: 'ProfileView' })
import { ref, reactive, onMounted, computed } from 'vue'
import Card from 'primevue/card'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import SelectButton from 'primevue/selectbutton'
import Dialog from 'primevue/dialog'
import Divider from 'primevue/divider'
import ToggleSwitch from 'primevue/toggleswitch'
import IftaLabel from 'primevue/iftalabel'
import Message from 'primevue/message'
import router from '@/router'
import {
  deleteUser,
  getUser,
  logout as apiLogout,
  updateUser,
  type UpdateRequest,
  type User,
} from '@/api/AuthAPI'

import { useI18n } from 'vue-i18n'
import { useNsfwContentStore } from '@/stores/nsfwContent'
type Language = 'en' | 'ru'
const { t, locale } = useI18n()
const nsfwContentStore = useNsfwContentStore()

import { useApiError } from '@/composables/useApiError'
const apiError = useApiError()

const currentUser = ref<User>()
const updateForm = reactive<UpdateRequest>({
  name: null,
  email: '',
  currentPassword: '',
  newPassword: '',
})
const isDirty = computed(() => {
  if (!currentUser.value) return false

  return updateForm.email !== currentUser.value.email
})

const isLoading = ref<boolean>(false)
const errorText = ref<string>('')
const submitUserData = async () => {
  try {
    errorText.value = ''
    isLoading.value = true
    if (!currentUser.value) return

    await updateUser({
      name: null,
      email: updateForm.email,
      currentPassword: null,
      newPassword: null,
    })

    Object.assign(currentUser.value, {
      email: updateForm.email,
    })
  } catch (e) {
    errorText.value = apiError.resolveMessage(e)
  } finally {
    isLoading.value = false
  }
}

const language = ref<Language>(locale.value === 'en' ? 'en' : 'ru')
const languageOptions = [
  { label: 'English', value: 'en' },
  { label: 'Russian', value: 'ru' },
]

function applyLanguage(lang: Language) {
  locale.value = lang
  localStorage.setItem('locale', lang)
}

const isPasswordLoading = ref<boolean>(false)
const passwordErrorText = ref<string>('')
const isPasswordDialogVisible = ref(false)
async function changePassword() {
  try {
    passwordErrorText.value = ''
    isPasswordLoading.value = true
    await updateUser({
      name: null,
      email: null,
      currentPassword: updateForm.currentPassword,
      newPassword: updateForm.newPassword,
    })
    isPasswordDialogVisible.value = false
  } catch (e) {
    passwordErrorText.value = apiError.resolveMessage(e)
  } finally {
    isPasswordLoading.value = false
  }
}

async function logout() {
  await apiLogout()
  router.push('/login')
}

const isDeleteDialogVisible = ref(false)
async function deleteAccount() {
  await deleteUser()
  await apiLogout()
  router.push('/login')
}

onMounted(async () => {
  currentUser.value = await getUser()
  Object.assign(updateForm, currentUser.value)

  const savedLocale = localStorage.getItem('locale') as Language | null
  if (savedLocale) {
    language.value = savedLocale
    locale.value = savedLocale
  }
})
</script>

<template>
  <div class="profile-view-root">
    <h1 class="ds-page-header">{{ $t('profile-view.profile') }}</h1>
    <Card>
      <template #content>
        <IftaLabel>
          <InputText id="email" v-model="updateForm.email" fluid />
          <label for="email">{{ $t('profile-view.email') }}</label>
        </IftaLabel>

        <div class="submit-button">
          <Button v-if="isDirty" label="Submit" :loading="isLoading" @click="submitUserData" />
        </div>

        <Message v-if="errorText.length" severity="error" icon="pi pi-times-circle" :life="3000">
          {{ errorText }}
        </Message>

        <Divider />

        <RouterLink to="/appearance" class="text-action"
          >{{ t('redesign.appearance') }} <i class="pi pi-angle-right"
        /></RouterLink>

        <div class="selector">
          <div class="selector-label">
            <label>{{ $t('profile-view.language') }}</label>
          </div>
          <SelectButton
            v-model="language"
            :options="languageOptions"
            optionLabel="label"
            optionValue="value"
            @change="applyLanguage(language)"
          />
        </div>

        <Divider />

        <div class="selector selector--nsfw">
          <div class="selector-label">
            <label>{{ $t('profile-view.nsfw-mode') }}</label>
            <span class="selector-hint">{{ $t('profile-view.nsfw-mode-hint') }}</span>
          </div>
          <ToggleSwitch
            :model-value="nsfwContentStore.showNsfwContent"
            @update:model-value="nsfwContentStore.setShowNsfwContent"
          />
        </div>

        <Divider />

        <div class="actions">
          <Button
            :label="t('profile-view.change-password')"
            severity="secondary"
            @click="isPasswordDialogVisible = true"
          />
          <Button :label="t('profile-view.logout')" severity="secondary" @click="logout" />
          <Button
            :label="t('profile-view.delete-account')"
            severity="danger"
            outlined
            @click="isDeleteDialogVisible = true"
          />
        </div>
      </template>
    </Card>

    <Dialog
      v-model:visible="isPasswordDialogVisible"
      :header="t('profile-view.change-password')"
      modal
    >
      <Password
        v-model="updateForm.currentPassword"
        :placeholder="t('current-password')"
        toggleMask
        fluid
      />
      <Password
        v-model="updateForm.newPassword"
        :placeholder="t('new-password')"
        toggleMask
        fluid
      />
      <template #footer>
        <Button :label="t('cancel')" text @click="isPasswordDialogVisible = false" />
        <Button :label="t('change')" :loading="isPasswordLoading" @click="changePassword" />
      </template>
    </Dialog>

    <Dialog
      v-model:visible="isDeleteDialogVisible"
      :header="t('profile-view.delete-account')"
      modal
    >
      <p>{{ $t('profile-view.delete-confirmation') }}</p>
      <template #footer>
        <Button :label="t('cancel')" text @click="isDeleteDialogVisible = false" />
        <Button :label="t('tasks.editdialog.delete')" severity="danger" @click="deleteAccount" />
      </template>
    </Dialog>
  </div>
</template>

<style scoped>
.profile-view-root {
  width: 100%;
}

:deep(.p-iftalabel) {
  margin-top: 0.5rem;
}

.actions {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.selector-label {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  align-self: center;
  gap: 0.25rem;
  max-width: min(100%, 20rem);
}

.selector--nsfw {
  align-items: center;
}

.selector-hint {
  font-size: 0.8rem;
  color: var(--p-text-muted-color);
  line-height: 1.3;
}

.selector {
  display: flex;
  justify-content: space-between;
  margin-bottom: 1rem;
}

.color-selector {
  flex-direction: column;
  align-items: flex-start;
  gap: 0.5rem;
}

.color-swatches {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.swatch {
  width: 1.75rem;
  height: 1.75rem;
  border-radius: 50%;
  border: 2px solid var(--p-surface-border);
  padding: 0;
  cursor: pointer;
  transition:
    transform 0.15s ease,
    box-shadow 0.15s ease;
}

.swatch:hover {
  transform: scale(1.1);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
}

.swatch-selected {
  border: 2px solid var(--p-text-color);
  box-shadow: 0 0 0 1px var(--p-surface-ground);
}

.swatch:focus-visible {
  outline: 2px solid var(--p-primary-color);
  outline-offset: 2px;
}

.submit-button {
  display: flex;
  justify-content: center;
  padding: 0.5rem;
}

@media (min-width: 900px) {
  .profile-view-root {
    max-width: 48rem;
    margin: 0 auto;
  }

  .profile-view-root .ds-page-header {
    margin-bottom: 1rem;
  }

  .selector {
    align-items: center;
    gap: 2rem;
  }

  .color-selector {
    flex-direction: row;
    align-items: center;
  }

  .actions {
    flex-direction: row;
    flex-wrap: wrap;
    justify-content: flex-end;
  }

  .submit-button {
    justify-content: flex-end;
    padding-right: 0;
  }

  :deep(.p-dialog) {
    width: min(30rem, calc(100vw - 2rem));
  }
}
</style>
