import type { HabitWithHistory } from "@/models/HabitItem";

const mockHabits: HabitWithHistory[] = [
    {
        habit: {
            id: 1,
            title: 'Drink water',
            color: '#ff0000'
        },
        history: [
            {
                date: new Date("January 1, 2026"),
                completion: 'full'
            },
            {
                date: new Date("January 2, 2026"),
                completion: 'full'
            },
            {
                date: new Date("January 3, 2026"),
                completion: 'skip'
            },
            {
                date: new Date("January 4, 2026"),
                completion: 'full'
            },

        ]
    },
    {
        habit: {
            id: 2,
            title: 'Sport',
            color: '#0000ff'
        },
        history: [
            {
                date: new Date("January 1, 2026"),
                completion: 'full'
            },
            {
                date: new Date("January 3, 2026"),
                completion: 'full'
            },
            {
                date: new Date("January 4, 2026"),
                completion: 'skip'
            },

        ]
    }
];

export const habitsApi = {
    async getHabits(): Promise<HabitWithHistory[]> {
        return new Promise(resolve => {
            setTimeout(() => resolve(mockHabits), 300)
        })
    },
}