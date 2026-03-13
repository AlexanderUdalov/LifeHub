export type TaskViewMode = 'standard' | 'compact' | 'calendar'

const TASK_VIEW_MODE_STORAGE_KEY = 'task-view-mode'

const VALID_MODES: TaskViewMode[] = ['standard', 'compact', 'calendar']

function isValidMode(raw: string | null): raw is TaskViewMode {
  return raw !== null && VALID_MODES.includes(raw as TaskViewMode)
}

export function getStoredTaskViewMode(): TaskViewMode | null {
  const raw = localStorage.getItem(TASK_VIEW_MODE_STORAGE_KEY)
  return isValidMode(raw) ? raw : null
}

export function setStoredTaskViewMode(mode: TaskViewMode): void {
  localStorage.setItem(TASK_VIEW_MODE_STORAGE_KEY, mode)
}
