import { useAuthStore } from "@/stores/auth";
import { api } from "./API";
import type { components } from "./schema";

type LoginRequest = components["schemas"]["LoginRequest"];
type RegisterRequest = components["schemas"]["RegisterRequest"];
export type UpdateRequest = components["schemas"]["UpdateRequest"];
export type User = components["schemas"]["UserDTO"];
type AuthResponse = components["schemas"]["AuthResponse"];

export async function login(request: LoginRequest) {
    const { data } = await api.post<AuthResponse>("/auth/login", request);
    return data;
}

export async function register(request: RegisterRequest) {
    const { data } = await api.post<AuthResponse>("/auth/register", request);
    return data;
}

export async function logout() {
    const auth = useAuthStore();
    if (auth.refreshToken) {
        await api.post("/auth/logout", { refreshToken: auth.refreshToken }, { skipAuthRefresh: true } as any);
    }
    auth.logout();
}

export async function deleteUser() {
    await api.delete("/auth");
}

export async function getUser() {
    const { data } = await api.get<User>("/auth");
    return data;
}

export async function updateUser(request: UpdateRequest) {
    const { data } = await api.patch<User>("/auth", request);
    return data;
}
