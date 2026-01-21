<script setup lang="ts">
import { ref } from "vue";
import { useAuthStore } from "@/stores/auth";
import { useRouter } from "vue-router";
import InputText from "primevue/inputtext";
import Password from "primevue/password";
import Button from "primevue/button";
import { login, register } from "@/api/AuthAPI";
import type { LoginRequest, RegisterRequest } from "@/api/AuthAPI";

const email = ref("");
const password = ref("");
const name = ref("");
const isRegister = ref(false);

const auth = useAuthStore();
const router = useRouter();

const submit = async () => {
    try {
        if (isRegister.value) {
            const request: RegisterRequest = {
                name: name.value,
                email: email.value,
                password: password.value
            }
            await register(request);
        }

        const loginRequest: LoginRequest = {
            email: email.value,
            password: password.value
        }
        const token = await login(loginRequest);
        auth.setToken(token);
        router.push("/");
    } catch (e) {
        console.error(e);
    }
};
</script>

<template>
    <div class="auth-form">
        <h2>{{ isRegister ? "Register" : "Login" }}</h2>

        <InputText v-if="isRegister" v-model="name" placeholder="Name" />
        <InputText v-model="email" placeholder="Email" />
        <Password v-model="password" toggleMask />

        <Button label="Submit" @click="submit" />
        <Button text :label="isRegister ? 'Already have account?' : 'Create account'"
            @click="isRegister = !isRegister" />
    </div>
</template>
