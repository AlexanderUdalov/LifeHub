<script setup lang="ts">
import { computed, ref } from "vue";
import { useAuthStore } from "@/stores/auth";
import { useRouter } from "vue-router";
import InputText from "primevue/inputtext";
import Password from "primevue/password";
import Button from "primevue/button";
import Card from 'primevue/card'
import { Form } from '@primevue/forms';
import { login, register } from "@/api/AuthAPI";
import { useI18n } from "vue-i18n";
import { useApiError } from "@/composables/useApiError";

const email = ref<string>("");
const password = ref<string>("");
const name = ref<string>("");
const isRegister = ref<boolean>(false);
const isLoading = ref<boolean>(false);
const errorText = ref<string>("");

const auth = useAuthStore();
const router = useRouter();
const { t } = useI18n()

const title = computed(() => isRegister ? t('auth.registration') : t('auth.login'));
const submitButtonText = computed(() => isLoading.value ? t("auth.processing") + "..." : title.value);
const toggleModeText = computed(() =>
    isRegister.value ? t('auth.registerToLogin') : t('auth.loginToRegister')
);

const submit = async () => {
    try {
        isLoading.value = true;
        let token: string;
        if (isRegister.value) {
            token = await register({
                name: name.value,
                email: email.value,
                password: password.value
            });
        }
        else {
            token = await login({
                email: email.value,
                password: password.value
            });
        }

        auth.setToken(token);
        router.push("/");
    }
    catch (error) {
        errorText.value = useApiError().resolveMessage(error)
    }
    finally {
        isLoading.value = false;
    }
};
</script>

<template>
    <Form @submit="submit">
        <Card>
            <template #title>{{ title }}</template>

            <template #content>
                <InputText v-if="isRegister" v-model="name" placeholder="Name" />
                <InputText v-model="email" placeholder="Email" />
                <Password v-model="password" toggleMask fluid />
                <Button text :label="toggleModeText" @click="isRegister = !isRegister" />
                <Message v-if="errorText.length" severity="error" :life="3000">{{ errorText }}</Message>
            </template>

            <template #footer>
                <Button type="submit" :label="submitButtonText" icon="pi pi-check" :loading="isLoading" />
            </template>
        </Card>
    </Form>
</template>

<style scoped>
:deep(.p-card-title) {
    text-align: center;
}

:deep(.p-card-content) {
    display: flex;
    flex-direction: column;
    margin: 10px;
    gap: 10px;
}

:deep(.p-card-footer) {
    display: flex;
    justify-content: flex-end;
    margin-right: 10px;
}
</style>
