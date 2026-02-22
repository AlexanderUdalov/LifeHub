import { api } from './API'
import type { components } from './schema'
import { toDateOnlyString } from '@/utils/dateOnly'

export type AddictionDTO = components['schemas']['AddictionDTO']
export type AddictionWithResetsDTO = components['schemas']['AddictionWithResetsDTO']
export type AddictionUpsertRequest = components['schemas']['AddictionUpsertRequest']

export const addictionsApi = {
  async getAddictions(days = 14): Promise<AddictionWithResetsDTO[]> {
    const { data } = await api.get<AddictionWithResetsDTO[]>('/addictions', { params: { days } })
    return data ?? []
  },

  async createAddiction(request: AddictionUpsertRequest): Promise<AddictionDTO> {
    const { data } = await api.post<AddictionDTO>('/addictions', request)
    return data!
  },

  async updateAddiction(id: string, request: AddictionUpsertRequest): Promise<AddictionDTO> {
    const { data } = await api.put<AddictionDTO>(`/addictions/${id}`, request)
    return data!
  },

  async deleteAddiction(id: string): Promise<void> {
    await api.delete(`/addictions/${id}`)
  },

  async setReset(addictionId: string, date: Date): Promise<void> {
    const dateOnly = toDateOnlyString(date)
    await api.put(`/addictions/${addictionId}/resets/${dateOnly}`)
  },

  async removeReset(addictionId: string, date: Date): Promise<void> {
    const dateOnly = toDateOnlyString(date)
    await api.delete(`/addictions/${addictionId}/resets/${dateOnly}`)
  }
}
