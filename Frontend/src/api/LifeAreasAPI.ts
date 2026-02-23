import { api } from './API'
import type { components } from './schema'

export type LifeAreaDTO = components['schemas']['LifeAreaDTO']
export type CreateLifeAreaRequest = components['schemas']['CreateLifeAreaRequest']
export type UpdateLifeAreaRequest = components['schemas']['UpdateLifeAreaRequest']

export const lifeAreasApi = {
  async getLifeAreas(): Promise<LifeAreaDTO[]> {
    const { data } = await api.get<LifeAreaDTO[]>('/lifeareas')
    return data ?? []
  },

  async getLifeArea(id: string): Promise<LifeAreaDTO> {
    const { data } = await api.get<LifeAreaDTO>(`/lifeareas/${id}`)
    return data
  },

  async createLifeArea(request: CreateLifeAreaRequest): Promise<LifeAreaDTO> {
    const { data } = await api.post<LifeAreaDTO>('/lifeareas', request)
    return data
  },

  async updateLifeArea(id: string, request: UpdateLifeAreaRequest): Promise<LifeAreaDTO> {
    const { data } = await api.put<LifeAreaDTO>(`/lifeareas/${id}`, request)
    return data
  },

  async deleteLifeArea(id: string): Promise<void> {
    await api.delete(`/lifeareas/${id}`)
  }
}
