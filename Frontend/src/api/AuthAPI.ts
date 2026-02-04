import { api } from "./API";
import type { components } from "./schema";

export type LoginRequest = components["schemas"]["LoginRequest"];
export type RegisterRequest = components["schemas"]["RegisterRequest"];
export type UpdateRequest = components["schemas"]["UpdateRequest"];
export type User = components["schemas"]["UserDTO"];
type AuthResponse = components["schemas"]["AuthResponse"];

export async function login(request: LoginRequest) {
    const { data } = await api.post<AuthResponse>("/auth/login", request);
    return data.token as string;
}

export async function register(request: RegisterRequest) {
    const { data } = await api.post<AuthResponse>("/auth/register", request);
    return data.token as string;
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
