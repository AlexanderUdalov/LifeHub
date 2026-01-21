import { api } from "./API";
import type { components } from "./schema";

export type LoginRequest = components["schemas"]["LoginRequest"];
export type RegisterRequest = components["schemas"]["RegisterRequest"];
type AuthResponse = components["schemas"]["AuthResponse"];

export async function login(request: LoginRequest) {
    const { data } = await api.post<AuthResponse>("/auth/login", request);
    return data.token as string;
}

export async function register(request: RegisterRequest) {
    await api.post("/auth/register", request);
}
