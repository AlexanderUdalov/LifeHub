import { api } from './API';
import type { components } from './schema';

export type StartReflectionRequest = components['schemas']['StartReflectionRequest'];
export type StartReflectionResponse = components['schemas']['StartReflectionResponse'];
export type SendReflectionMessageRequest = components['schemas']['SendReflectionMessageRequest'];
export type ReflectionMessageResponse = components['schemas']['ReflectionMessageResponse'];
export type ChatMessageDTO = components['schemas']['ChatMessageDTO'];

export async function startReflection(periodDays: number): Promise<StartReflectionResponse> {
    const { data } = await api.post<StartReflectionResponse>('/ai/reflection/start', {
        periodDays
    });
    return data;
}

export async function sendReflectionMessage(
    contextId: string,
    messages: ChatMessageDTO[]
): Promise<ReflectionMessageResponse> {
    const { data } = await api.post<ReflectionMessageResponse>('/ai/reflection/message', {
        contextId,
        messages
    });
    return data;
}
