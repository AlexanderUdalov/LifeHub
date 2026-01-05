export interface HabitItem {
    id: number;
    title: string;
    color: string;
}

export type HabitCompletion = 'none' | 'skip' | 'full'

export interface HabitDayEntry {
    date: Date;
    completion: HabitCompletion;
}

export interface HabitWithHistory {
    habit: HabitItem
    history: HabitDayEntry[]
}