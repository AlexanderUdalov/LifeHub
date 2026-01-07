import type { GoalItem } from "@/models/GoalItem";

const mockGoals: GoalItem[] = [
    {
        id: 1,
        title: 'Healthy body',
        dueDate: new Date('2026-03-01'),
        area: {
            title: 'Health',
            icon: 'pi pi-heart',
            color: '#e53935'
        },
        progress: 65,
        tasks: [1, 2],
        habits: [1],
        addictions: [1]
    }
];

export const goalsApi = {
    async getGoals(): Promise<GoalItem[]> {
        return new Promise(resolve => {
            setTimeout(() => resolve(mockGoals), 300)
        })
    },
}