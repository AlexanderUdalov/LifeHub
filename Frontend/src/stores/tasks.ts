import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { rrulestr } from 'rrule'
import { createTask, deleteTask, getTasks, getCompletedTasks, updateTask, type CreateTaskRequest, type TaskDTO, type UpdateTaskRequest } from '@/api/TasksAPI'
import { isToday, startOfDay, startOfUtcDay, toUtcDateOnlyIso } from '@/utils/dateOnly'

const COMPLETED_PAGE_SIZE = 20

function getNextRecurrenceDate(ruleStr: string | null | undefined, afterDate: Date, dtstart?: Date): Date | null {
    const str = ruleStr?.trim()
    if (!str) return null
    try {
        const fullStr = str.toUpperCase().startsWith('RRULE:') ? str : `RRULE:${str}`

        // rrule MONTHLY behaves like it matches BYMONTHDAY in UTC.
        // To preserve "calendar day" semantics, we build dtstart/after as UTC-midnight
        // for the chosen local Y/M/D.
        const afterUtcMidnight = startOfUtcDay(afterDate)
        const dtstartUtcMidnight = dtstart ? startOfUtcDay(dtstart) : undefined

        const rule = rrulestr(fullStr, { dtstart: dtstartUtcMidnight ?? afterUtcMidnight, unfold: true })
        const next = rule.after(afterUtcMidnight, false)
        return next ? startOfUtcDay(next) : null
    } catch {
        return null
    }
}

export const useTasksStore = defineStore('tasks', () => {
    /** Active (non-completed) tasks only. */
    const tasks = ref<TaskDTO[]>([])
    /** Completed tasks: loaded slots have TaskDTO, unloaded slots are undefined (length = completedTotal for virtual scroll). */
    const completedTasks = ref<(TaskDTO | undefined)[]>([])
    const completedTotal = ref(0)
    const isLoading = ref(false)
    const isLoadingMoreCompleted = ref(false)

    async function fetchTasks() {
        isLoading.value = true
        try {
            const [active, completedPage] = await Promise.all([
                getTasks(),
                getCompletedTasks(COMPLETED_PAGE_SIZE, 0)
            ])
            tasks.value = active
            completedTotal.value = completedPage.total
            completedTasks.value = [
                ...completedPage.items,
                ...Array(Math.max(0, completedPage.total - completedPage.items.length)).fill(undefined)
            ]
        } finally {
            isLoading.value = false
        }
    }

    /** Load more completed tasks for the range [first, last] (inclusive). Called by VirtualScroller lazy-load. */
    async function fetchMoreCompletedTasks(first: number, last: number) {
        const count = last - first + 1
        if (count <= 0) return
        const current = completedTasks.value
        const needLoad = current.slice(first, last + 1).some(t => t === undefined)
        if (!needLoad) return
        isLoadingMoreCompleted.value = true
        try {
            const page = await getCompletedTasks(count, first)
            const next: (TaskDTO | undefined)[] = [...current]
            for (let i = 0; i < page.items.length; i++) {
                next[first + i] = page.items[i]
            }
            completedTasks.value = next
        } finally {
            isLoadingMoreCompleted.value = false
        }
    }

    async function createNewTask(request: CreateTaskRequest) {
        const created = await createTask(request)
        tasks.value.push(created)
    }

    async function editTask(taskId: string, request: UpdateTaskRequest) {
        let existing: TaskDTO | undefined = tasks.value.find(t => t.id === taskId)
        if (!existing) {
            const idx = completedTasks.value.findIndex(t => t?.id === taskId)
            existing = idx !== -1 ? completedTasks.value[idx] : undefined
        }
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
        let index = tasks.value.findIndex(t => t.id === taskId)
        if (index !== -1) {
            const backup = tasks.value[index]
            tasks.value.splice(index, 1)
            try {
                await deleteTask(taskId)
            } catch (e) {
                if (backup) tasks.value.push(backup)
                throw e
            }
            return
        }
        const completedIdx = completedTasks.value.findIndex(t => t?.id === taskId)
        if (completedIdx === -1) return
        const backup = completedTasks.value[completedIdx]
        completedTasks.value = completedTasks.value.slice(0, completedIdx).concat(completedTasks.value.slice(completedIdx + 1))
        completedTotal.value = Math.max(0, completedTotal.value - 1)
        try {
            await deleteTask(taskId)
        } catch (e) {
            const next = [...completedTasks.value]
            next.splice(completedIdx, 0, backup)
            completedTasks.value = next
            completedTotal.value = completedTotal.value + 1
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
    const weekTasks = computed(() => {
        const list = tasks.value.filter(t => !t.completionDate && t.dueDate && isThisWeek(new Date(t.dueDate)) && !overdueTasks.value.includes(t) && !todayTasks.value.includes(t))
        return list.sort((a, b) => {
            const dateA = new Date(a.dueDate!).getTime()
            const dateB = new Date(b.dueDate!).getTime()
            if (dateA !== dateB) return dateA - dateB
            return sortByOrder(a.sortOrder as number, b.sortOrder as number)
        })
    })
    /** Completed tasks for display: defined slots from completedTasks ref (order already by completion date from API). */
    const completedTasksList = computed(() =>
        completedTasks.value.filter((t): t is TaskDTO => t !== undefined && t !== null)
    )
    const inboxTasks = computed(() => {
        const list = tasks.value.filter(t => !t.completionDate && !t.dueDate)
        return list.sort((a, b) => sortByOrder(a.sortOrder as number, b.sortOrder as number))
    })

    function sortByOrder(a: number | null | undefined, b: number | null | undefined): number {
        if (a == null && b == null) return 0
        if (a == null) return 1
        if (b == null) return -1
        return a - b
    }

    async function toggleTaskCompletion(taskId: string, completed: boolean) {
        let task: TaskDTO | undefined = tasks.value.find(t => t.id === taskId)
        if (!task) {
            const idx = completedTasks.value.findIndex(t => t?.id === taskId)
            task = idx !== -1 ? completedTasks.value[idx] : undefined
        }
        if (!task) return

        const previousCompletionDate = task.completionDate
        task.completionDate = completed ? new Date().toISOString() : null

        if (completed) {
            tasks.value = tasks.value.filter(t => t.id !== taskId)
            completedTasks.value = [task, ...completedTasks.value]
            completedTotal.value = completedTotal.value + 1
        } else {
            const idx = completedTasks.value.findIndex(t => t?.id === taskId)
            if (idx !== -1) {
                completedTasks.value = completedTasks.value.slice(0, idx).concat(completedTasks.value.slice(idx + 1))
                completedTotal.value = Math.max(0, completedTotal.value - 1)
                tasks.value.push(task)
            }
        }

        await delay(400)

        try {
            if (completed && task.recurrenceRule?.trim()) {
                // Compute the next occurrence after the *scheduled* due date.
                // Using `new Date()` (moment of clicking) can cause the next task
                // to land on the next immediate day if the user completes the task
                // before the scheduled moment.
                const due = task.dueDate ? new Date(task.dueDate) : undefined
                const afterDate = due ?? new Date()
                const nextDate = getNextRecurrenceDate(task.recurrenceRule, afterDate, due)
                if (nextDate) {
                    await createNewTask({
                        title: task.title,
                        description: task.description,
                        dueDate: toUtcDateOnlyIso(nextDate),
                        recurrenceRule: task.recurrenceRule,
                        goalId: task.goalId,
                        lifeAreaId: task.lifeAreaId
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
                lifeAreaId: task.lifeAreaId,
                sortOrder: task.sortOrder
            })
        } catch {
            task.completionDate = previousCompletionDate
            if (completed) {
                tasks.value.push(task)
                const i = completedTasks.value.findIndex(t => t?.id === taskId)
                if (i !== -1) {
                    const arr = completedTasks.value.slice(0, i).concat(completedTasks.value.slice(i + 1))
                    completedTasks.value = arr
                    completedTotal.value = Math.max(0, completedTotal.value - 1)
                }
            } else {
                tasks.value = tasks.value.filter(t => t.id !== taskId)
                completedTasks.value = [{ ...task, completionDate: new Date().toISOString() } as TaskDTO, ...completedTasks.value]
                completedTotal.value = completedTotal.value + 1
            }
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
                    lifeAreaId: t.lifeAreaId,
                    sortOrder: i
                })
            }
        }
    }

    return {
        tasks,
        completedTasks,
        completedTotal,
        completedTasksList,
        isLoading,
        isLoadingMoreCompleted,
        fetchTasks,
        fetchMoreCompletedTasks,
        editTask,
        removeTask,
        createNewTask,
        toggleTaskCompletion,
        reorderTasksInList,
        todayTasks,
        overdueTasks,
        weekTasks,
        inboxTasks
    }
})
