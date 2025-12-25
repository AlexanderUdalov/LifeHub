import type { TaskItem } from "@/models/TaskItem";

const mockTasks: TaskItem[] = [
    {
        id: 1,
        title: 'Buy gym membership',
        isCompleted: false,
        dueDate: new Date("December 25, 2025 13:30:00")
    },
    {
        id: 2,
        title: 'Install calorie tracker',
        description: 'Find a good app for daily calorie tracking',
        isCompleted: true
    },
    {
        id: 3,
        title: 'Test3',
        description: 'Find a good app for daily calorie tracking',
        isCompleted: false
    },
    {
        id: 4,
        title: 'Test4',
        isCompleted: true,
        dueDate: new Date("December 25, 2025")
    }
];

export const tasksApi = {
    async getTasks(): Promise<TaskItem[]> {
        return new Promise(resolve => {
            setTimeout(() => resolve(mockTasks), 300)
        })
    },
}