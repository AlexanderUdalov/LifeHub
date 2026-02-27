import { api } from './API';
import type { components } from './schema';

export type JournalEntryDTO = components['schemas']['JournalEntryDTO'];
export type CreateJournalEntryRequest = components['schemas']['CreateJournalEntryRequest'];

export type UpdateJournalEntryPayload = Partial<components['schemas']['UpdateJournalEntryRequest']>;

export async function getJournalEntries(): Promise<JournalEntryDTO[]> {
    const { data } = await api.get<JournalEntryDTO[]>('/journal');
    return data;
}

export async function createJournalEntry(request: CreateJournalEntryRequest): Promise<JournalEntryDTO> {
    const { data } = await api.post<JournalEntryDTO>('/journal', request);
    return data;
}

export async function updateJournalEntry(id: string, request: UpdateJournalEntryPayload): Promise<JournalEntryDTO> {
    const { data } = await api.put<JournalEntryDTO>(`/journal/${id}`, request);
    return data;
}

export async function deleteJournalEntry(id: string): Promise<void> {
    await api.delete(`/journal/${id}`);
}
