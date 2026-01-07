export interface GoalItem {
    id: number
    title: string
    dueDate: Date
    area: {
        title: string
        icon: string
        color: string
    }
    progress: number
    tasks: number[]
    habits: number[]
    addictions: number[]
}