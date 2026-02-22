import axios, { type InternalAxiosRequestConfig } from "axios";
import { useAuthStore } from "@/stores/auth";
import router from "@/router";

declare module "axios" {
    interface AxiosRequestConfig {
        skipAuthRefresh?: boolean;
    }
}

export const api = axios.create({
    baseURL: "/api"
});

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
    const auth = useAuthStore();
    if (auth.token) config.headers.Authorization = `Bearer ${auth.token}`;
    return config;
});

let refreshPromise: Promise<string> | null = null;

function doRefresh(): Promise<string> {
    const auth = useAuthStore();
    if (!auth.refreshToken) return Promise.reject(new Error("No refresh token"));
    interface RefreshResponse {
        token: string;
        refreshToken: string;
    }
    return api
        .post<RefreshResponse>("/auth/refresh", { refreshToken: auth.refreshToken }, { skipAuthRefresh: true })
        .then((res) => {
            const data = res.data as RefreshResponse;
            auth.setTokens(data.token, data.refreshToken);
            return data.token;
        });
}

api.interceptors.response.use(
    (response) => response,
    (error) => {
        if (error.response?.status !== 401) {
            return Promise.reject(error);
        }
        const auth = useAuthStore();

        if (error.config?.skipAuthRefresh) {
            auth.logout();
            router.push("/login");
            return Promise.reject(error);
        }

        if (!auth.refreshToken) {
            auth.logout();
            router.push("/login");
            return Promise.reject(error);
        }

        refreshPromise = refreshPromise ?? doRefresh().finally(() => { refreshPromise = null; });

        return refreshPromise
            .then((token) => {
                error.config.headers.Authorization = `Bearer ${token}`;
                return api.request(error.config);
            })
            .catch((err) => {
                auth.logout();
                router.push("/login");
                return Promise.reject(err);
            });
    }
);
