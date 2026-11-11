import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { getCompletedTasks, getTasks, type TaskDTO } from '@/api/TasksAPI'
import { useTasksStore } from '../tasks'

vi.mock('@/api/TasksAPI', () => ({
  getTasks: vi.fn(), getCompletedTasks: vi.fn(), createTask: vi.fn(),
  updateTask: vi.fn(), deleteTask: vi.fn(),
}))

function task(i: number): TaskDTO {
  return { id: String(i), title: `Task ${i}`, completionDate: '2026-11-11T10:00:00Z' } as TaskDTO
}

beforeEach(() => {
  vi.resetAllMocks()
  setActivePinia(createPinia())
  vi.mocked(getTasks).mockResolvedValue([])
})

describe('completed task pagination', () => {
  it('loads every page once and preserves the total and ordering', async () => {
    const items = Array.from({ length: 51 }, (_, i) => task(i))
    vi.mocked(getCompletedTasks).mockImplementation(async (limit, offset) => ({
      items: items.slice(offset, offset + limit), total: items.length,
    }))
    const store = useTasksStore()
    await store.fetchTasks()
    expect(store.completedTasksList).toHaveLength(20)
    expect(store.completedTotal).toBe(51)
    await store.fetchMoreCompletedTasks(20, 39)
    await store.fetchMoreCompletedTasks(40, 50)
    await store.fetchMoreCompletedTasks(20, 39)
    expect(store.completedTasksList).toEqual(items)
    expect(getCompletedTasks).toHaveBeenCalledTimes(3)
  })

  it('prevents duplicate in-flight page requests', async () => {
    vi.mocked(getCompletedTasks).mockResolvedValueOnce({ items: [task(0)], total: 2 })
    const store = useTasksStore()
    await store.fetchTasks()
    let resolve!: (page: { items: TaskDTO[]; total: number }) => void
    vi.mocked(getCompletedTasks).mockReturnValueOnce(new Promise(r => { resolve = r }))
    const pending = store.fetchMoreCompletedTasks(1, 1)
    await store.fetchMoreCompletedTasks(1, 1)
    expect(getCompletedTasks).toHaveBeenCalledTimes(2)
    resolve({ items: [task(1)], total: 2 })
    await pending
    expect(store.completedTasksList).toHaveLength(2)
  })

  it('keeps loaded tasks after an error and permits retry', async () => {
    vi.mocked(getCompletedTasks).mockResolvedValueOnce({ items: [task(0)], total: 2 })
    const store = useTasksStore()
    await store.fetchTasks()
    vi.mocked(getCompletedTasks).mockRejectedValueOnce(new Error('offline'))
    await expect(store.fetchMoreCompletedTasks(1, 1)).rejects.toThrow('offline')
    expect(store.isLoadingMoreCompleted).toBe(false)
    expect(store.completedTasksList).toEqual([task(0)])
    vi.mocked(getCompletedTasks).mockResolvedValueOnce({ items: [task(1)], total: 2 })
    await store.fetchMoreCompletedTasks(1, 1)
    expect(store.completedTasksList).toEqual([task(0), task(1)])
  })

  it('accepts a reduced server total without requesting empty pages forever', async () => {
    vi.mocked(getCompletedTasks).mockResolvedValueOnce({ items: [task(0)], total: 3 })
    const store = useTasksStore()
    await store.fetchTasks()
    vi.mocked(getCompletedTasks).mockResolvedValueOnce({ items: [], total: 1 })
    await store.fetchMoreCompletedTasks(1, 2)
    expect(store.completedTotal).toBe(1)
    expect(store.completedTasks).toEqual([task(0)])
  })

  it('does not overwrite a refreshed list with a stale page', async () => {
    vi.mocked(getCompletedTasks).mockResolvedValueOnce({ items: [task(0)], total: 2 })
    const store = useTasksStore()
    await store.fetchTasks()
    let resolve!: (page: { items: TaskDTO[]; total: number }) => void
    vi.mocked(getCompletedTasks).mockReturnValueOnce(new Promise(r => { resolve = r }))
    const pending = store.fetchMoreCompletedTasks(1, 1)
    vi.mocked(getCompletedTasks).mockResolvedValueOnce({ items: [task(9)], total: 1 })
    await store.fetchTasks()
    resolve({ items: [task(1)], total: 2 })
    await pending
    expect(store.completedTasks).toEqual([task(9)])
    expect(store.completedTotal).toBe(1)
  })
})
