import type { paths, components } from "./schema";
import axios from "axios";

const api = axios.create({
    baseURL: "/api",
    withCredentials: true
});

export type TaskDTO = components["schemas"]["TaskDTO"];

export type CreateTaskRequest = components["schemas"]["CreateTaskRequest"];
export type UpdateTaskRequest = components["schemas"]["UpdateTaskRequest"];

export async function getTasks(): Promise<TaskDTO[]> {
    const { data } = await api.get<TaskDTO[]>("/tasks");
    return data;
}

export async function createTask(request: CreateTaskRequest): Promise<TaskDTO> {
    const { data } = await api.post<TaskDTO>("/tasks", request);
    return data;
}

export async function updateTask(id: string, request: UpdateTaskRequest): Promise<TaskDTO> {
    const { data } = await api.put<TaskDTO>(`/tasks/${id}`, request);
    return data;
}

export async function deleteTask(id: string): Promise<void> {
    await api.delete(`/tasks/${id}`);
}