import { api } from './API'
import type { components } from './schema'

export type DayFeedbackDTO = components['schemas']['DayFeedbackDTO']
export type SaveDayFeedbackRequest = components['schemas']['SaveDayFeedbackRequest']

export async function getDayFeedback(date: string): Promise<DayFeedbackDTO | null> {
  const { data, status } = await api.get<DayFeedbackDTO>(`/day-feedback/${date}`)
  return status === 204 ? null : data
}

export async function saveDayFeedback(date: string, request: SaveDayFeedbackRequest): Promise<DayFeedbackDTO> {
  const { data } = await api.put<DayFeedbackDTO>(`/day-feedback/${date}`, request)
  return data
}
