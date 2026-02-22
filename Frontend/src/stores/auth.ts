import { defineStore } from "pinia";
import { computed, ref } from "vue";

const TOKEN_KEY = "token";
const REFRESH_TOKEN_KEY = "refreshToken";

export const useAuthStore = defineStore("auth", () => {
    const token = ref(localStorage.getItem(TOKEN_KEY) as string | null);
    const refreshToken = ref(localStorage.getItem(REFRESH_TOKEN_KEY) as string | null);

    const isAuthenticated = computed(() => !!token.value);

    function setTokens(accessToken: string, newRefreshToken: string) {
        token.value = accessToken;
        refreshToken.value = newRefreshToken;
        localStorage.setItem(TOKEN_KEY, accessToken);
        localStorage.setItem(REFRESH_TOKEN_KEY, newRefreshToken);
    }

    function setToken(newToken: string) {
        token.value = newToken;
        localStorage.setItem(TOKEN_KEY, newToken);
    }

    function logout() {
        token.value = null;
        refreshToken.value = null;
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(REFRESH_TOKEN_KEY);
    }

    return { token, refreshToken, isAuthenticated, setTokens, setToken, logout };
});
