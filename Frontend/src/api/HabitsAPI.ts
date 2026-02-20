import { api } from './API'
import type { components } from './schema'
import { toDateOnlyString } from '@/utils/dateOnly'

export type HabitDTO = components['schemas']['HabitDTO']
export type HabitDayDTO = components['schemas']['HabitDayDTO']
export type HabitWithHistoryDTO = components['schemas']['HabitWithHistoryDTO']
export type HabitUpsertRequest = components['schemas']['HabitUpsertRequest']

export const habitsApi = {
  async getHabits(days = 14): Promise<HabitWithHistoryDTO[]> {
    const { data } = await api.get<HabitWithHistoryDTO[]>('/habits', { params: { days } })
    return data ?? []
  },

  async createHabit(request: HabitUpsertRequest): Promise<HabitDTO> {
    const { data } = await api.post<HabitDTO>('/habits', request)
    return data
  },

  async updateHabit(id: string, request: HabitUpsertRequest): Promise<HabitDTO> {
    const { data } = await api.put<HabitDTO>(`/habits/${id}`, request)
    return data
  },

  async deleteHabit(id: string): Promise<void> {
    await api.delete(`/habits/${id}`)
  },

  async setDayStatus(habitId: string, date: Date, status: string): Promise<HabitDayDTO> {
    const dateOnly = toDateOnlyString(date)
    const { data } = await api.put<HabitDayDTO>(`/habits/${habitId}/days/${dateOnly}`, { status })
    return data
  }
}