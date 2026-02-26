import { api } from './API'

export interface GoalDTO {
  id: string
  title: string
  description: string | null
  dueDate: string
  lifeAreaId: string | null
}

export interface CreateGoalRequest {
  title: string
  description: string | null
  dueDate: string
  lifeAreaId: string | null
}

export interface UpdateGoalRequest {
  title: string | null
  description: string | null
  dueDate: string | null
  lifeAreaId: string | null
}

export const goalsApi = {
  async getGoals(): Promise<GoalDTO[]> {
    const { data } = await api.get<GoalDTO[]>('/goals')
    return data ?? []
  },

  async getGoal(id: string): Promise<GoalDTO> {
    const { data } = await api.get<GoalDTO>(`/goals/${id}`)
    return data
  },

  async createGoal(request: CreateGoalRequest): Promise<GoalDTO> {
    const { data } = await api.post<GoalDTO>('/goals', request)
    return data
  },

  async updateGoal(id: string, request: UpdateGoalRequest): Promise<GoalDTO> {
    const { data } = await api.put<GoalDTO>(`/goals/${id}`, request)
    return data
  },

  async deleteGoal(id: string): Promise<void> {
    await api.delete(`/goals/${id}`)
  }
}