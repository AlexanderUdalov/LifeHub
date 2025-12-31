import type { JournalItem } from '@/models/JournalItem';

const mockEntries: JournalItem[] = [
    {
        id: 1,
        text: '',
        date: new Date("December 25, 2025 13:30:00")
    },
    {
        id: 2,
        text: '',
        goalId: 1,
        date: new Date("December 26, 2025 13:30:00")
    },
    {
        id: 3,
        text: '',
        addictionId: 1,
        date: new Date("December 29, 2025")
    },
    {
        id: 4,
        text: '',
        goalId: 1,
        addictionId: 1,
        date: new Date("December 20, 2025")
    },
];

export const journalApi = {
    async getItems(): Promise<JournalItem[]> {
        return new Promise(resolve => {
            setTimeout(() => resolve(mockEntries), 300)
        })
    },
}