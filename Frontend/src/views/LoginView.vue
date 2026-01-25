<script setup lang="ts">
import { useI18n } from "vue-i18n";
const { t } = useI18n()

import { computed, ref } from "vue";
import { useAuthStore } from "@/stores/auth";
import { useRouter } from "vue-router";
import { z } from 'zod'
import { zodResolver } from '@primevue/forms/resolvers/zod'

import InputText from "primevue/inputtext";
import Password from "primevue/password";
import Button from "primevue/button";
import Card from 'primevue/card'
import FloatLabel from 'primevue/floatlabel'
import Message from 'primevue/message'

import { Form, FormField } from '@primevue/forms'
import { login, register } from "@/api/AuthAPI";

import { useApiError } from "@/composables/useApiError";
const apiError = useApiError();

const resolver = computed(() =>
    zodResolver(
        isRegister.value
            ? z.object({
                nickname: z.string(t('auth.requiredField')),
                email: z.email(t('auth.requiredField')),
                password: z.string(t('auth.requiredField'))
            })
            : z.object({
                email: z.email(t('auth.requiredField')),
                password: z.string(t('auth.requiredField'))
            })
    )
);

const isRegister = ref<boolean>(false);
const isLoading = ref<boolean>(false);
const errorText = ref<string>("");

const auth = useAuthStore();
const router = useRouter();

const title = computed(() => isRegister.value ? t('auth.registration') : t('auth.login'));
const submitButtonText = computed(() => isLoading.value ? t("auth.processing") + "..." : title.value);
const toggleModeText = computed(() =>
    isRegister.value ? t('auth.registerToLogin') : t('auth.loginToRegister')
);
const submit = async ({ valid, values }: any) => {
    if (!valid) return
    try {
        errorText.value = "";
        isLoading.value = true;
        let token: string;
        if (isRegister.value) {
            token = await register({
                name: values.nickname,
                email: values.email,
                password: values.password
            });
        }
        else {
            token = await login({
                email: values.email,
                password: values.password
            });
        }

        auth.setToken(token);
        router.push("/");
    }
    catch (e) {
        errorText.value = apiError.resolveMessage(e)
    }
    finally {
        isLoading.value = false;
    }
};
</script>

<template>
    <Form :resolver @submit="submit" :validateOnBlur="true">
        <Card>
            <template #title>{{ title }}</template>

            <template #content>
                <FormField v-if="isRegister" v-slot="$field" name="nickname">
                    <FloatLabel variant="on">
                        <InputText id="name" type="text" fluid />
                        <label for="name">{{ $t('auth.nickname') }}</label>
                    </FloatLabel>
                    <Message v-if="$field?.invalid" severity="error" size="small" variant="simple">
                        {{ $field.error?.message }}
                    </Message>
                </FormField>

                <FormField v-slot="$field" name="email">
                    <FloatLabel variant="on">
                        <InputText id="email" fluid />
                        <label for="email">{{ $t('auth.email') }}</label>
                    </FloatLabel>
                    <Message v-if="$field?.invalid" severity="error" size="small" variant="simple">
                        {{ $field.error?.message }}
                    </Message>
                </FormField>

                <FormField v-slot="$field" name="password">
                    <FloatLabel variant="on">
                        <Password id="password" toggleMask :feedback="false" fluid />
                        <label for="password">{{ $t('auth.password') }}</label>
                    </FloatLabel>
                    <Message v-if="$field?.invalid" severity="error" size="small" variant="simple">
                        {{ $field.error?.message }}
                    </Message>
                </FormField>

                <Button text :label="toggleModeText" @click="isRegister = !isRegister" />

                <Message v-if="errorText.length" severity="error" icon="pi pi-times-circle" :life="3000">
                    {{ errorText }}
                </Message>
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
