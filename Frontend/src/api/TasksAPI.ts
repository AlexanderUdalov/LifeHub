import { api } from "./API";
import type { paths, components } from "./schema";

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