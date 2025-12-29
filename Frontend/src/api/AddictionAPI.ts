import type { AddictionItem } from "@/models/AddictionItem";

const mockAddictions: AddictionItem[] = [
    {
        id: 1,
        title: 'Smoking',
        icon: 'pi-ban',
        lastReset: new Date("December 25, 2025 13:30:00")
    },
    {
        id: 2,
        title: 'Drinking',
        icon: 'pi-ban',
        lastReset: new Date("December 20, 2025 13:30:00")
    },
    {
        id: 3,
        title: 'Serfing',
        icon: 'pi-ban',
        lastReset: new Date()
    }
];

export const addictionsApi = {
    async getAddictions(): Promise<AddictionItem[]> {
        return new Promise(resolve => {
            setTimeout(() => resolve(mockAddictions), 300)
        })
    },
}