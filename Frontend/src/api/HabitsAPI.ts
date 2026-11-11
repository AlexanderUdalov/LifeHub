import { api } from './API'
import type { components } from './schema'
import { toDateOnlyString } from '@/utils/dateOnly'

export type HabitDTO = components['schemas']['HabitDTO']
export type HabitDayDTO = components['schemas']['HabitDayDTO']
export type HabitWithHistoryDTO = components['schemas']['HabitWithHistoryDTO']
export type HabitUpsertRequest = components['schemas']['HabitUpsertRequest']
export type HabitHistoryDTO = components['schemas']['HabitHistoryDTO']
export type HabitState = 'active' | 'paused' | 'archived'
export type HabitCompletion = 'none' | 'skip' | 'full'
const today = () => toDateOnlyString(new Date())

export const habitsApi = {
  async getHabits(days = 56): Promise<HabitWithHistoryDTO[]> {
    const { data } = await api.get<HabitWithHistoryDTO[]>('/habits', {
      params: { days, today: today() },
    })
    return data ?? []
  },
  async getHistory(id: string, before?: string): Promise<HabitHistoryDTO> {
    const { data } = await api.get<HabitHistoryDTO>(`/habits/${id}/history`, {
      params: { before, days: 56, today: today() },
    })
    return data
  },
  async createHabit(request: HabitUpsertRequest): Promise<HabitDTO> {
    const { data } = await api.post<HabitDTO>('/habits', request, { params: { today: today() } })
    return data
  },
  async updateHabit(id: string, request: HabitUpsertRequest): Promise<HabitDTO> {
    const { data } = await api.put<HabitDTO>(`/habits/${id}`, request, {
      params: { today: today() },
    })
    return data
  },
  async setState(
    id: string,
    state: HabitState,
  ): Promise<components['schemas']['HabitStateUpdateDTO']> {
    const { data } = await api.put(`/habits/${id}/state`, { state }, { params: { today: today() } })
    return data
  },
  async deleteHabit(id: string): Promise<void> {
    await api.delete(`/habits/${id}`)
  },
  async setDayStatus(
    id: string,
    date: Date,
    status: HabitCompletion,
  ): Promise<components['schemas']['HabitDayUpdateDTO']> {
    const { data } = await api.put(
      `/habits/${id}/days/${toDateOnlyString(date)}`,
      { status },
      { params: { today: today() } },
    )
    return data
  },
}
