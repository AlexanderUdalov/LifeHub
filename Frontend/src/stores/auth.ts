import { defineStore } from "pinia";
import { computed, ref } from "vue";

export const useAuthStore = defineStore("auth", () => {
    const token = ref(localStorage.getItem("token") as string | null);

    const isAuthenticated = computed(() => !!token.value);

    function setToken(newToken: string) {
        token.value = newToken;
        localStorage.setItem("token", newToken);
    }

    function logout() {
        token.value = null;
        localStorage.removeItem("token");
    }

    return { token, isAuthenticated, setToken, logout };
});
