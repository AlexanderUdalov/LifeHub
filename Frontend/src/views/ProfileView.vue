<script setup lang="ts">
import { ref, reactive, onMounted, computed } from 'vue'
import Card from 'primevue/card'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import SelectButton from 'primevue/selectbutton'
import Dialog from 'primevue/dialog'
import Divider from 'primevue/divider'
import IftaLabel from 'primevue/iftalabel'
import Message from 'primevue/message'
import { useAuthStore } from '@/stores/auth'
import router from "@/router";
import { deleteUser, getUser, updateUser, type UpdateRequest, type User } from '@/api/AuthAPI'

import { useI18n } from 'vue-i18n'
type Language = 'en' | 'ru'
const { t, locale } = useI18n()

import { useApiError } from "@/composables/useApiError";
const apiError = useApiError();

const currentUser = ref<User>();
const updateForm = reactive<UpdateRequest>({
    name: '',
    email: '',
    currentPassword: '',
    newPassword: ''
})
const isDirty = computed(() => {
    if (!currentUser.value) return false

    return (
        updateForm.name !== currentUser.value.name ||
        updateForm.email !== currentUser.value.email
    )
})

const isLoading = ref<boolean>(false)
const errorText = ref<string>('')
const submitUserData = async () => {
    try {
        errorText.value = "";
        isLoading.value = true;
        if (!currentUser.value) return

        await updateUser({
            name: updateForm.name,
            email: updateForm.email,
            currentPassword: null,
            newPassword: null
        })

        Object.assign(currentUser.value, {
            name: updateForm.name,
            email: updateForm.email
        })
    }
    catch (e) {
        errorText.value = apiError.resolveMessage(e)
    }
    finally {
        isLoading.value = false;
    }
}

import { applyTheme, getStoredTheme, setStoredTheme, type ThemeMode } from '@/utils/theme'

const theme = ref<ThemeMode>(getStoredTheme() ?? 'auto')
const themeOptions = [
    { label: 'Light', value: 'light' },
    { label: 'Dark', value: 'dark' },
    { label: 'Auto', value: 'auto' }
]

function onThemeChange(mode: ThemeMode) {
    setStoredTheme(mode)
    applyTheme(mode)
}


const language = ref<Language>('en')
const languageOptions = [
    { label: 'English', value: 'en' },
    { label: 'Russian', value: 'ru' }
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
        passwordErrorText.value = "";
        isPasswordLoading.value = true;
        await updateUser({
            name: null,
            email: null,
            currentPassword: updateForm.currentPassword,
            newPassword: updateForm.newPassword
        })
        isPasswordDialogVisible.value = false;
    }
    catch (e) {
        passwordErrorText.value = apiError.resolveMessage(e)
    }
    finally {
        isPasswordLoading.value = false;
    }
}

const auth = useAuthStore();
function logout() {
    auth.logout()
    router.push('/login')
}

const isDeleteDialogVisible = ref(false)
async function deleteAccount() {
    await deleteUser()
    router.push('/register')
}

onMounted(async () => {
    currentUser.value = await getUser();
    Object.assign(updateForm, currentUser.value)

    const savedLocale = localStorage.getItem('locale') as Language | null
    if (savedLocale) {
        language.value = savedLocale
        locale.value = savedLocale
    }

    const savedTheme = getStoredTheme()
    if (savedTheme) {
        theme.value = savedTheme
        applyTheme(savedTheme)
    }
})
</script>

<template>
    <Card>
        <template #title>{{ $t('profile-view.profile') }}</template>

        <template #content>
            <IftaLabel>
                <InputText id="name" v-model="updateForm.name" fluid />
                <label for="name">{{ $t('profile-view.name') }}</label>
            </IftaLabel>

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

            <div class="selector">
                <div class="selector-label">
                    <label>{{ $t('profile-view.theme') }}</label>
                </div>
                <SelectButton v-model="theme" :options="themeOptions" optionLabel="label" optionValue="value"
                    @change="onThemeChange(theme)" />
            </div>

            <Divider />

            <div class="selector">
                <div class="selector-label">
                    <label>{{ $t('profile-view.language') }}</label>
                </div>
                <SelectButton v-model="language" :options="languageOptions" optionLabel="label" optionValue="value"
                    @change="applyLanguage(language)" />
            </div>

            <Divider />

            <div class="actions">
                <Button :label="t('profile-view.change-password')" severity="secondary"
                    @click="isPasswordDialogVisible = true" />
                <Button :label="t('profile-view.logout')" severity="secondary" @click="logout" />
                <Button :label="t('profile-view.delete-account')" severity="danger" outlined
                    @click="isDeleteDialogVisible = true" />
            </div>
        </template>
    </Card>

    <Dialog v-model:visible="isPasswordDialogVisible" :header="t('profile-view.change-password')" modal>
        <Password v-model="updateForm.currentPassword" :placeholder="t('current-password')" toggleMask fluid />
        <Password v-model="updateForm.newPassword" :placeholder="t('new-password')" toggleMask fluid />
        <template #footer>
            <Button :label="t('cancel')" text @click="isPasswordDialogVisible = false" />
            <Button :label="t('change')" :loading="isPasswordLoading" @click="changePassword" />
        </template>
    </Dialog>

    <Dialog v-model:visible="isDeleteDialogVisible" :header="t('profile-view.delete-account')" modal>
        <p>{{ $t('profile-view.delete-confirmation') }}</p>
        <template #footer>
            <Button :label="t('cancel')" text @click="isDeleteDialogVisible = false" />
            <Button :label="t('tasks.editdialog.delete')" severity="danger" @click="deleteAccount" />
        </template>
    </Dialog>
</template>

<style scoped>
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
    align-items: center;
}

.selector {
    display: flex;
    justify-content: space-around;
}

.submit-button {
    display: flex;
    justify-content: center;
    padding: 0.5rem;
}
</style>
