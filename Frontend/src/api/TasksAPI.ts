import { api } from "./API";
import type { components } from "./schema";

export type TaskDTO = components["schemas"]["TaskDTO"];

export type CreateTaskRequest = components["schemas"]["CreateTaskRequest"];
export type UpdateTaskRequest = components["schemas"]["UpdateTaskRequest"];

/** Response of GET /tasks/completed (paginated). Re-export from schema after api:gen. */
interface CompletedTasksPageResponse {
    items: TaskDTO[];
    total: number;
}

/** Returns only active (non-completed) tasks. */
export async function getTasks(): Promise<TaskDTO[]> {
    const { data } = await api.get<TaskDTO[]>("/tasks");
    return data;
}

/** Returns a page of completed tasks for lazy loading. Sorted by completion date descending. */
export async function getCompletedTasks(limit: number, offset: number): Promise<CompletedTasksPageResponse> {
    const { data } = await api.get<CompletedTasksPageResponse>("/tasks/completed", {
        params: { limit, offset }
    });
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