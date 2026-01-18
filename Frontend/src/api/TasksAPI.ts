import type { TaskItem } from "@/models/TaskItem";

const mockTasks: TaskItem[] = [
    {
        id: 1,
        title: 'Buy gym membership',
        dueDate: new Date("December 25, 2025 13:30:00")
    },
    {
        id: 2,
        title: 'Install calorie tracker',
        description: 'Find a good app for daily calorie tracking',
        completeDate: new Date("December 28, 2025")
    },
    {
        id: 3,
        title: 'Test3',
        description: 'Find a good app for daily calorie tracking',
        dueDate: new Date("December 30, 2025 13:30:00")
    },
    {
        id: 4,
        title: 'Test4',
        completeDate: new Date("December 28, 2025"),
        dueDate: new Date("December 25, 2025")
    }
];

export const tasksApi = {
    async getTasks(): Promise<TaskItem[]> {
        return new Promise(resolve => {
            setTimeout(() => resolve(mockTasks), 300)
        })
    },

    async saveTask(task: TaskItem): Promise<TaskItem> {
        return new Promise(resolve => {
            setTimeout(() => {
                if (task.id != null) {
                    const index = mockTasks.findIndex(t => t.id === task.id);
                    if (index !== -1) {
                        mockTasks[index] = { ...mockTasks[index], ...task };
                        resolve(mockTasks[index]);
                        return;
                    }
                }

                const newTask: TaskItem = {
                    ...task,
                    id: mockTasks.length
                        ? Math.max(...mockTasks.map(t => t.id)) + 1
                        : 1,
                };

                mockTasks.push(newTask);
                resolve(newTask);
            }, 300);
        });
    }
}