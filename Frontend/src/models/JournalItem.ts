export interface JournalItem {
    id: number;
    text: string;
    goalId?: number;
    addictionId?: number;
    date: Date;
}