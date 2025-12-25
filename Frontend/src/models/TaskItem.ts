export interface TaskItem
{
    id: number;
    title: string;
    description?: string;
    dueDate?: Date;
    isCompleted: boolean;
}