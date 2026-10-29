import { api } from './API';
import type { components } from './schema';

type StartReflectionRequest = components['schemas']['StartReflectionRequest'];
type StartReflectionResponse = components['schemas']['StartReflectionResponse'];
type ReflectionMessageResponse = components['schemas']['ReflectionMessageResponse'];
export type ChatMessageDTO = components['schemas']['ChatMessageDTO'];

export async function startReflection(
    periodDays: number,
    language?: StartReflectionRequest['language']
): Promise<StartReflectionResponse> {
    const { data } = await api.post<StartReflectionResponse>('/ai/reflection/start', {
        periodDays,
        language
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
