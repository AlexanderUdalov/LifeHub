export interface TaskItem
{
    id: number;
    title: string;
    description?: string;
    dueDate?: Date;
    completeDate?: Date;
}