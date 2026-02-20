import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { rrulestr } from 'rrule'
import { createTask, deleteTask, getTasks, updateTask, type CreateTaskRequest, type TaskDTO, type UpdateTaskRequest } from '@/api/TasksAPI'
import { isToday } from '@/utils/dateOnly'

function getNextRecurrenceDate(ruleStr: string | null | undefined, afterDate: Date, dtstart?: Date): Date | null {
    const str = ruleStr?.trim()
    if (!str) return null
    try {
        const fullStr = str.toUpperCase().startsWith('RRULE:') ? str : `RRULE:${str}`
        const rule = rrulestr(fullStr, { dtstart: dtstart ?? afterDate, unfold: true })
        const next = rule.after(afterDate, false)
        return next ?? null
    } catch {
        return null
    }
}

export const useTasksStore = defineStore('tasks', () => {
    const tasks = ref<TaskDTO[]>([])
    const isLoading = ref(false)

    async function fetchTasks() {
        isLoading.value = true
        try {
            tasks.value = await getTasks()
        } finally {
            isLoading.value = false
        }
    }

    async function createNewTask(request: CreateTaskRequest) {
        const created = await createTask(request)
        tasks.value.push(created)
    }

    async function editTask(taskId: string, request: UpdateTaskRequest) {
        const existing = tasks.value.find(t => t.id === taskId)
        if (!existing) return

        const backup = { ...existing }

        Object.assign(existing, request)

        try {
            await updateTask(taskId, request)
        } catch (e) {
            Object.assign(existing, backup)
            throw e
        }
    }

    async function removeTask(taskId: string) {
        const index = tasks.value.findIndex(t => t.id === taskId)
        if (index === -1) return

        const backup = tasks.value[index]
        tasks.value.splice(index, 1)

        try {
            await deleteTask(taskId)
        } catch (e) {
            if (backup)
                tasks.value.push(backup)
            throw e
        }
    }

    function isThisWeek(date: Date) {
        const now = new Date()
        const start = new Date(now)
        start.setDate(now.getDate() - now.getDay())
        const end = new Date(start)
        end.setDate(start.getDate() + 7)
        return date >= start && date < end
    }

    const todayTasks = computed(() => {
        const list = tasks.value.filter(t => !t.completionDate && t.dueDate && isToday(new Date(t.dueDate)))
        return list.sort((a, b) => sortByOrder(a.sortOrder as number, b.sortOrder as number))
    })
    const overdueTasks = computed(() => tasks.value.filter(t => !t.completionDate && t.dueDate && new Date(t.dueDate) < new Date() && !todayTasks.value.includes(t)))
    const weekTasks = computed(() => tasks.value.filter(t => !t.completionDate && t.dueDate && isThisWeek(new Date(t.dueDate)) && !overdueTasks.value.includes(t) && !todayTasks.value.includes(t)))
    const completedTasks = computed(() => {
        const list = tasks.value.filter(t => t.completionDate)
        return list.sort((a, b) => (new Date(b.completionDate!).getTime()) - (new Date(a.completionDate!).getTime()))
    })
    const inboxTasks = computed(() => {
        const list = tasks.value.filter(t => !overdueTasks.value.includes(t) && !todayTasks.value.includes(t) && !weekTasks.value.includes(t) && !completedTasks.value.includes(t))
        return list.sort((a, b) => sortByOrder(a.sortOrder as number, b.sortOrder as number))
    })

    function sortByOrder(a: number | null | undefined, b: number | null | undefined): number {
        if (a == null && b == null) return 0
        if (a == null) return 1
        if (b == null) return -1
        return a - b
    }

    async function toggleTaskCompletion(taskId: string, completed: boolean) {
        const task = tasks.value.find(t => t.id === taskId)
        if (!task) return

        const previousCompletionDate = task.completionDate
        task.completionDate = completed ? new Date().toISOString() : null

        await delay(400)

        try {
            if (completed && task.recurrenceRule?.trim()) {
                const afterDate = new Date()
                const dtstart = task.dueDate ? new Date(task.dueDate) : undefined
                const nextDate = getNextRecurrenceDate(task.recurrenceRule, afterDate, dtstart)
                if (nextDate) {
                    await createNewTask({
                        title: task.title,
                        description: task.description,
                        dueDate: nextDate.toISOString(),
                        recurrenceRule: task.recurrenceRule,
                        goalId: task.goalId
                    })
                }
            }

            await updateTask(task.id, {
                title: task.title,
                description: task.description,
                dueDate: task.dueDate,
                completionDate: task.completionDate,
                recurrenceRule: task.recurrenceRule,
                goalId: task.goalId,
                sortOrder: task.sortOrder
            })
        } catch {
            task.completionDate = previousCompletionDate
        }
    }

    function delay(ms: number) {
        return new Promise(resolve => setTimeout(resolve, ms))
    }

    async function reorderTasksInList(orderedTasks: TaskDTO[]) {
        for (let i = 0; i < orderedTasks.length; i++) {
            const t = tasks.value.find(x => x.id === orderedTasks[i]?.id)
            if (t && t.sortOrder != i) {
                t.sortOrder = i
                await updateTask(t.id, {
                    title: t.title,
                    description: t.description,
                    dueDate: t.dueDate,
                    completionDate: t.completionDate,
                    recurrenceRule: t.recurrenceRule,
                    goalId: t.goalId,
                    sortOrder: i
                })
            }
        }
    }

    return {
        tasks,
        isLoading,
        fetchTasks,
        editTask,
        removeTask,
        createNewTask,
        toggleTaskCompletion,
        reorderTasksInList,
        todayTasks,
        overdueTasks,
        weekTasks,
        inboxTasks,
        completedTasks
    }
})
