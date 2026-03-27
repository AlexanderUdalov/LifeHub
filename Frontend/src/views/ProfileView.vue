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
import IftaLabel from 'primevue/iftalabel'
import Message from 'primevue/message'
import router from "@/router";
import { deleteUser, getUser, logout as apiLogout, updateUser, type UpdateRequest, type User } from "@/api/AuthAPI";

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

import { applyTheme, getStoredTheme, setStoredTheme, getStoredPrimaryColor, getStoredSurfaceColor, applyPrimaryColor, applySurfaceColor, type ThemeMode } from '@/utils/theme'

const DEFAULT_PRIMARY_HEX = '#10b981'
const DEFAULT_SURFACE_HEX = '#71717a'

// Preset colors like on https://primevue.org (Primary / Surface)
const PRIMARY_PRESETS: { name: string; hex: string }[] = [
    { name: 'Emerald', hex: '#10b981' },
    { name: 'Green', hex: '#22c55e' },
    { name: 'Lime', hex: '#84cc16' },
    { name: 'Yellow', hex: '#eab308' },
    { name: 'Orange', hex: '#f97316' },
    { name: 'Red', hex: '#ef4444' },
    { name: 'Pink', hex: '#ec4899' },
    { name: 'Purple', hex: '#a855f7' },
    { name: 'Violet', hex: '#8b5cf6' },
    { name: 'Indigo', hex: '#6366f1' },
    { name: 'Blue', hex: '#3b82f6' },
    { name: 'Sky', hex: '#0ea5e9' }
]

const SURFACE_PRESETS: { name: string; hex: string }[] = [
    { name: 'Zinc', hex: '#71717a' },
    { name: 'Slate', hex: '#64748b' },
    { name: 'Gray', hex: '#6b7280' },
    { name: 'Neutral', hex: '#737373' },
    { name: 'Stone', hex: '#78716c' }
]

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

const primaryColor = ref<string>(getStoredPrimaryColor() ?? DEFAULT_PRIMARY_HEX)
const surfaceColor = ref<string>(getStoredSurfaceColor() ?? DEFAULT_SURFACE_HEX)

function selectPrimary(hex: string) {
    primaryColor.value = hex
    onPrimaryColorChange(hex)
}

function selectSurface(hex: string) {
    surfaceColor.value = hex
    onSurfaceColorChange(hex)
}

function isPrimarySelected(hex: string) {
    return primaryColor.value?.toLowerCase() === hex.toLowerCase()
}

function isSurfaceSelected(hex: string) {
    return surfaceColor.value?.toLowerCase() === hex.toLowerCase()
}

function onPrimaryColorChange(hex: string) {
    applyPrimaryColor(hex)
}

function onSurfaceColorChange(hex: string) {
    applySurfaceColor(hex)
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

async function logout() {
    await apiLogout();
    router.push("/login");
}

const isDeleteDialogVisible = ref(false);
async function deleteAccount() {
    await deleteUser();
    await apiLogout();
    router.push("/register");
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
    primaryColor.value = getStoredPrimaryColor() ?? DEFAULT_PRIMARY_HEX
    surfaceColor.value = getStoredSurfaceColor() ?? DEFAULT_SURFACE_HEX

})
</script>

<template>
    <div class="profile-view-root">
        <h1 class="ds-page-header">{{ $t('profile-view.profile') }}</h1>
        <Card>
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

                <div class="selector color-selector">
                    <div class="selector-label">
                        <label>{{ $t('profile-view.primary-color') }}</label>
                    </div>
                    <div class="color-swatches" role="listbox" :aria-label="$t('profile-view.primary-color')">
                        <button v-for="preset in PRIMARY_PRESETS" :key="preset.hex" type="button" role="option"
                            :aria-selected="isPrimarySelected(preset.hex)" :title="preset.name" class="swatch"
                            :class="{ 'swatch-selected': isPrimarySelected(preset.hex) }"
                            :style="{ backgroundColor: preset.hex }" @click="selectPrimary(preset.hex)" />
                    </div>
                </div>

                <div class="selector color-selector">
                    <div class="selector-label">
                        <label>{{ $t('profile-view.surface-color') }}</label>
                    </div>
                    <div class="color-swatches" role="listbox" :aria-label="$t('profile-view.surface-color')">
                        <button v-for="preset in SURFACE_PRESETS" :key="preset.hex" type="button" role="option"
                            :aria-selected="isSurfaceSelected(preset.hex)" :title="preset.name" class="swatch"
                            :class="{ 'swatch-selected': isSurfaceSelected(preset.hex) }"
                            :style="{ backgroundColor: preset.hex }" @click="selectSurface(preset.hex)" />
                    </div>
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
    align-items: center;
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
    transition: transform 0.15s ease, box-shadow 0.15s ease;
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
</style>
