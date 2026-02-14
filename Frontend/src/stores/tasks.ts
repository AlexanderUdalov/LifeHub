import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { createTask, deleteTask, getTasks, updateTask, type CreateTaskRequest, type TaskDTO, type UpdateTaskRequest } from '@/api/TasksAPI'

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

    function isToday(date: Date) {
        const today = new Date()
        return date.toDateString() === today.toDateString()
    }

    function isThisWeek(date: Date) {
        const now = new Date()
        const start = new Date(now)
        start.setDate(now.getDate() - now.getDay())
        const end = new Date(start)
        end.setDate(start.getDate() + 7)
        return date >= start && date < end
    }

    const todayTasks = computed(() => tasks.value.filter(t => !t.completionDate && t.dueDate && isToday(new Date(t.dueDate))))
    const overdueTasks = computed(() => tasks.value.filter(t => !t.completionDate && t.dueDate && new Date(t.dueDate) < new Date() && !todayTasks.value.includes(t)))
    const weekTasks = computed(() => tasks.value.filter(t => !t.completionDate && t.dueDate && isThisWeek(new Date(t.dueDate)) && !overdueTasks.value.includes(t) && !todayTasks.value.includes(t)))
    const completedTasks = computed(() => tasks.value.filter(t => t.completionDate))
    const inboxTasks = computed(() => tasks.value.filter(t => !overdueTasks.value.includes(t) && !todayTasks.value.includes(t) && !weekTasks.value.includes(t) && !completedTasks.value.includes(t)))

    async function toggleTaskCompletion(taskId: string, completed: boolean) {
        const task = tasks.value.find(t => t.id === taskId)
        if (!task) return

        const previousCompletionDate = task.completionDate
        task.completionDate = completed ? new Date().toISOString() : null

        await delay(400)

        try {
            await updateTask(task.id, {
                title: task.title,
                description: task.description,
                dueDate: task.dueDate,
                completionDate: task.completionDate,
                recurrenceRule: task.recurrenceRule,
                goalId: task.goalId
            })
        } catch {
            task.completionDate = previousCompletionDate
        }
    }

    function delay(ms: number) {
        return new Promise(resolve => setTimeout(resolve, ms))
    }

    return {
        tasks,
        isLoading,
        fetchTasks,
        editTask,
        removeTask,
        createNewTask,
        toggleTaskCompletion,
        todayTasks,
        overdueTasks,
        weekTasks,
        inboxTasks,
        completedTasks
    }
})
